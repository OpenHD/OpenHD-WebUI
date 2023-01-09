using JetBrains.Annotations;

namespace OpenHdWebUi.Server.Configuration;

#nullable disable
[UsedImplicitly]
public class SystemCommandConfiguration : IAirGroundSelector
{
    public string Id { get; set; }

    public string DisplayName { get; set; }

    public string Command { get; set; }

    public bool IsAirOnly { get; set; }

    public bool IsGroundOnly { get; set; }
}