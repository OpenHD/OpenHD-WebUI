﻿using JetBrains.Annotations;
#nullable disable warnings
// ReSharper disable NotNullOrRequiredMemberIsNotInitialized
// ReSharper disable CollectionNeverUpdated.Global

namespace OpenHdWebUi.Server.Configuration;
[UsedImplicitly]
public class ServiceConfiguration
{
    public string FilesFolder { get; set; }

    public List<SystemCommandConfiguration> SystemCommands { get; set; }

    public List<SystemFileConfiguration> SystemFiles { get; set; }
}

[UsedImplicitly]
public record SystemFileConfiguration(string Id, string DisplayName,string Path);