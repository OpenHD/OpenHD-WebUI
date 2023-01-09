namespace OpenHdWebUi.Server.Configuration;

public interface IAirGroundSelector
{
    public bool IsAirOnly { get; }
    public bool IsGroundOnly { get; }
}