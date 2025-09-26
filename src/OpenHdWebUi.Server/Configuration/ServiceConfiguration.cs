using JetBrains.Annotations;
#nullable disable warnings
// ReSharper disable NotNullOrRequiredMemberIsNotInitialized
// ReSharper disable CollectionNeverUpdated.Global

namespace OpenHdWebUi.Server.Configuration;
[UsedImplicitly]
public class ServiceConfiguration
{
    public List<string> FilesFolders { get; set; }

    public List<SystemCommandConfiguration> SystemCommands { get; set; }

    public List<SystemFileConfiguration> SystemFiles { get; set; }

    public List<string> SettingsDirectories { get; set; }

    public UpdateFileConfiguration UpdateConfig { get; set; }
}

[UsedImplicitly]
public record SystemFileConfiguration(string Id, string DisplayName,string Path);

[UsedImplicitly]
public record UpdateFileConfiguration(string UpdateFile);