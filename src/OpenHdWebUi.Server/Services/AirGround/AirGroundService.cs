using OpenHdWebUi.Server.Configuration;

namespace OpenHdWebUi.Server.Services.AirGround;

public class AirGroundService
{
    private const string GroundMarkerPath = "/boot/openhd/ground.tx";

    private const string AirMarkerPath = "/boot/openhd/air.txt";

    public AirGroundService()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            IsAirMode = true;
            IsGroundMode = true;
            return;
        }

        IsAirMode = File.Exists(AirMarkerPath);
        IsGroundMode = File.Exists(GroundMarkerPath);
        if (!IsAirMode && !IsGroundMode)
        {
            IsAirMode = true;
            IsGroundMode = true;
        }
    }

    public bool IsAirMode { get; }

    public bool IsGroundMode { get; }

    public bool IsItemVisible(IAirGroundSelector item)
    {
        if (item is { IsAirOnly: true, IsGroundOnly: true } || item is { IsAirOnly: false, IsGroundOnly: false })
        {
            return true;
        }

        if (IsAirMode == item.IsAirOnly || IsGroundMode == item.IsGroundOnly)
        {
            return true;
        }

        return false;
    }
}
