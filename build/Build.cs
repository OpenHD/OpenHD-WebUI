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
    FetchDepth = 0,
    ImportSecrets = new []{ "CLOUDSMITH_API_KEY" })]
partial class Build : NukeBuild
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
        DownloadsPath = OutputPath / "downloads";
    }

    public static int Main () => Execute<Build>(x => x.Publish);

    [PathExecutable("dpkg-deb")]
    readonly Tool DpkgDeb;

    [PathExecutable("cloudsmith")]
    readonly Tool Cloudsmith;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Path for cloudsmith release repo", Name = "REPO")]
    readonly string Repo;

    [Parameter("Path for cloudsmith dev repo", Name = "DEV_REPO")]
    readonly string DevRepo;

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
        .DependsOn(UncompressFfmpeg)
        .Executes(() =>
        {
            foreach (var rid in Rids)
            {
                var arc = rid.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1];
                var linuxArc = ToLinuxArc(arc);
                var packageFolderName = $"{PackageName}_{CurrentVersion}_{linuxArc}";
                var debPackDirectory = DebBuildPath / packageFolderName;
                EnsureExistingDirectory(debPackDirectory);

                var serviceTargetDirectory = debPackDirectory / "usr" / "local" / "share" / "openhd" / "web-ui";
                CopyDirectoryRecursively(
                    GetPublishPathForRim(rid),
                    serviceTargetDirectory,
                    excludeFile: info => info.Name == "appsettings.Development.json");

                var packSystemDDir = debPackDirectory / "etc" / "systemd" / "system";
                EnsureExistingDirectory(packSystemDDir);
                CopyFile(RootDirectory / "openhd-web-ui.service", packSystemDDir / "openhd-web-ui.service");

                var debianDirectory = debPackDirectory / "DEBIAN";
                EnsureExistingDirectory(debianDirectory);
                CreateControlFile(RootDirectory / "control.template", debianDirectory / "control", CurrentVersion, linuxArc);

                CopyFile(RootDirectory / "postinst", debianDirectory / "postinst");

                if (IsUnix)
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    File.SetUnixFileMode(debianDirectory / "postinst", (UnixFileMode)509);
#pragma warning restore CA1416 // Validate platform compatibility
                }

                // Copy ffmpeg
                var ffmpegTargetFolder = serviceTargetDirectory / "ffmpeg";

                var binDescription = FfmpegBinDescriptions.Single(description => description.Rid == rid);
                CopyDirectoryRecursively(DownloadsPath / binDescription.UncompressedDirectoryName, ffmpegTargetFolder);
                if (IsUnix)
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    File.SetUnixFileMode(ffmpegTargetFolder / "ffmpeg", (UnixFileMode)509);
                    File.SetUnixFileMode(ffmpegTargetFolder / "ffprobe", (UnixFileMode)509);
#pragma warning restore CA1416 // Validate platform compatibility
                }
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
                var linuxArc = ToLinuxArc(arc);
                var packageFolderName = $"{PackageName}_{CurrentVersion}_{linuxArc}";
                DpkgDeb($"-Zxz --root-owner-group --build {packageFolderName}", DebBuildPath);
            }
        });

    Target CloudsmithPublish => _ => _
        .DependsOn(DebPack)
        .Executes(() =>
        {
            string repoName;
            if (string.Equals(GitVersion.BranchName, "master", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(GitVersion.BranchName, "main", StringComparison.InvariantCultureIgnoreCase))
            {
                repoName = Repo;
            }
            else
            {
                repoName = DevRepo;
            }

            foreach (var debFile in DebBuildPath.GlobFiles("*.deb"))
            {
                Cloudsmith($"push deb openhd/{repoName}/any-distro/any-version {debFile.Name}", DebBuildPath);
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

    static string ToLinuxArc(string original)
    {
        return original switch
        {
            "arm" => "armhf",
            "x64" => "amd64",
            _ => original
        };
    }
}
