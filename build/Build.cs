using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using System.Numerics;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(Publish) })]
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

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    readonly AbsolutePath OutputPath;
    readonly AbsolutePath PublishPath;
    readonly AbsolutePath DebBuildPath;

    [Solution(GenerateProjects = true)]
    readonly Solution Solution;
    Project PublishProject => Solution.GetProject("OpenHdWebUi.Server");

    IReadOnlyCollection<string> Rims;

    protected override void OnBuildInitialized()
    {
        Rims = PublishProject.GetRuntimeIdentifiers()
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
                .CombineWith(Rims, (_, s) => _
                    .SetOutput(PublishPath / s)
                    .SetRuntime(s)));
        });
}
