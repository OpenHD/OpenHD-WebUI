using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GlobExpressions;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using Stubble.Core.Builders;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(Clean), nameof(CloudsmithPublish) },
    AutoGenerate = true,
    FetchDepth = 0)]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public Build()
    {
        OutputPath = RootDirectory / "out";
        PublishPath = OutputPath / "publish";
        DebBuildPath = OutputPath / "deb";
    }

    public static int Main () => Execute<Build>(x => x.Publish);

    [PathExecutable("dpkg-deb")]
    readonly Tool DpkgDeb;

    [PathExecutable("cloudsmith")]
    readonly Tool Cloudsmith;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitVersion(NoFetch = true)]
    readonly GitVersion GitVersion;
    string CurrentVersion => GitVersion.FullSemVer;

    readonly AbsolutePath OutputPath;
    readonly AbsolutePath PublishPath;
    readonly AbsolutePath DebBuildPath;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;
    Project PublishProject => Solution.GetProject("OpenHdWebUi.Server");

    IReadOnlyCollection<string> Rids;

    const string PackageName = "open-hd-web-ui";

    protected override void OnBuildInitialized()
    {
        Rids = PublishProject.GetRuntimeIdentifiers()
            .Where(r => !r.StartsWith("win"))
            .ToList();
    }

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            EnsureCleanDirectory(OutputPath);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(settings => settings
                .SetProjectFile(Solution));
        });

    Target Publish => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetPublish(settings => settings
                .SetNoRestore(true)
                .SetProject(PublishProject)
                .SetConfiguration(Configuration)
                .SetSelfContained(true)
                .SetVersion(CurrentVersion)
                .CombineWith(Rids, (_, s) => _
                    .SetOutput(GetPublishPathForRim(s))
                    .SetRuntime(s)));
        });

    Target PrepareForDebPack => _ => _
        .DependsOn(Publish)
        .Executes(() =>
        {
            foreach (var rid in Rids)
            {
                var arc = rid.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1];
                var packageFolderName = $"{PackageName}_{CurrentVersion}_{arc}";
                var debPackDirectory = DebBuildPath / packageFolderName;
                EnsureExistingDirectory(debPackDirectory);

                CopyDirectoryRecursively(
                    GetPublishPathForRim(rid),
                    debPackDirectory / "usr" / "local" / "share" / "openhd" / "web-ui",
                    excludeFile: info => info.Name == "appsettings.Development.json");

                var packSystemDDir = debPackDirectory / "etc" / "systemd" / "system";
                EnsureExistingDirectory(packSystemDDir);
                CopyFile(RootDirectory / "OpenHDWebUI.service", packSystemDDir / "OpenHDWebUI.service");

                var controlFileDirectory = debPackDirectory / "DEBIAN";
                EnsureExistingDirectory(controlFileDirectory);
                CreateControlFile(RootDirectory / "control.template", controlFileDirectory / "control", CurrentVersion, arc);
            }
        });

    Target DebPack => _ => _
        .DependsOn(PrepareForDebPack)
        .OnlyWhenStatic(() => IsLinux)
        .Executes(() =>
        {
            foreach (var rid in Rids)
            {
                var arc = rid.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1];
                var packageFolderName = $"{PackageName}_{CurrentVersion}_{arc}";
                DpkgDeb($"--root-owner-group --build {packageFolderName}", DebBuildPath);
            }
        });

    Target CloudsmithPublish => _ => _
        .DependsOn(DebPack)
        .Executes(() =>
        {
            foreach (var debFile in DebBuildPath.GlobFiles("*.deb"))
            {
                Cloudsmith($"push deb bldzone/ohd-web-ui-dev/any-distro/any-version {debFile.Name}", DebBuildPath);
            }
        });

    AbsolutePath GetPublishPathForRim(string rid)
    {
        return PublishPath / rid;
    }

    static void CreateControlFile(
        AbsolutePath controlFileTemplatePath,
        AbsolutePath outputPath,
        string version,
        string arc)
    {
        var stubble = new StubbleBuilder().Build();
        var data = new Dictionary<string, object>
        {
            { "version", version },
            { "architecture", arc }
        };
        var rendered = stubble.Render(File.ReadAllText(controlFileTemplatePath), data);
        File.WriteAllText(outputPath, rendered);
    }
}
