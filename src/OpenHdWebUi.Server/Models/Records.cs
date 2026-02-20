namespace OpenHdWebUi.Server.Models;

public record SystemCommandDto(string Id, string DisplayName);

public record SystemFileDto(string Id, string DisplayName);

public record RunCommandRequest(string Id);

public record RunTerminalCommandRequest(string Command);

public record ServerFileInfo(string FileName, string DownloadPath);

public record AirGroundStatus(bool IsAir, bool IsGround);

public record LoginRequest(string Username, string Password);
public record WifiInterfaceDto(string Name, string Driver);

public record EthernetInterfaceDto(string Name, string IpAddress, string Netmask);

public record NetworkInfoDto(IReadOnlyCollection<WifiInterfaceDto> Wifi, IReadOnlyCollection<EthernetInterfaceDto> Ethernet);

public record SetIpRequest(string Interface, string Ip, string Netmask);

public record SettingFileSummaryDto(string Id, string Name, string RelativePath, string? Category);

public record SettingFileDto(string Id, string Name, string RelativePath, string? Category, string Content);

public record UpdateSettingFileRequest(string Content);

public record SysutilCameraInfoDto(bool IsAvailable, bool HasCameraType, int CameraType);

public record CameraSetupRequestDto(int CameraType);

public record CameraSetupResponseDto(bool Ok, bool Applied, string? Message);

public record RunModeInfoDto(bool IsAvailable, string Mode);

public record RunModeUpdateRequestDto(string Mode);

public record RunModeUpdateResponseDto(bool Ok, string? Message, string? Mode);

public record SysutilDebugDto(bool IsAvailable, bool Debug);

public record SysutilDebugUpdateRequestDto(bool Debug);

public record SysutilDebugUpdateResponseDto(bool Ok, bool Debug, string? Message);

public record SysutilPlatformInfoDto(bool IsAvailable, int PlatformType, string PlatformName);

public record SysutilPlatformUpdateRequestDto(string Action, int? PlatformType, string? PlatformName);

public record SysutilPlatformUpdateResponseDto(bool Ok, int PlatformType, string PlatformName, string? Message);

public record SysutilVideoResponseDto(bool Ok, string? Pipeline, string? Message);

public record OpenHdStatusDto(
    bool IsAvailable,
    bool HasData,
    bool HasError,
    string? State,
    string? Description,
    string? Message,
    int Severity,
    long UpdatedMs);
public record PlatformInfoDto(
    bool IsAvailable,
    int PlatformType,
    string PlatformName,
    string? Action)
{
    public static PlatformInfoDto Unavailable() => new(false, 0, "Unavailable", null);
}

public record PlatformUpdateRequest(string Action, int? PlatformType, string? PlatformName);

public record WifiCardInfoDto(
    string InterfaceName,
    string DriverName,
    string Mac,
    int PhyIndex,
    string VendorId,
    string DeviceId,
    string DetectedType,
    string OverrideType,
    string EffectiveType,
    bool Disabled,
    string TxPower,
    string TxPowerHigh,
    string TxPowerLow,
    string CardName,
    string PowerLevel,
    string PowerLowest,
    string PowerLow,
    string PowerMid,
    string PowerHigh,
    string PowerMin,
    string PowerMax);

public record WifiInfoDto(
    bool IsAvailable,
    IReadOnlyCollection<WifiCardInfoDto> Cards,
    string? Action)
{
    public static WifiInfoDto Unavailable() => new(false, Array.Empty<WifiCardInfoDto>(), null);
}

public record WifiUpdateRequest(
    string Action,
    string? Interface,
    string? OverrideType,
    string? TxPower,
    string? TxPowerHigh,
    string? TxPowerLow,
    string? CardName,
    string? PowerLevel);

public record WifiCardProfileDto(
    string VendorId,
    string DeviceId,
    string Name,
    string PowerMode,
    int MinMw,
    int MaxMw,
    int Lowest,
    int Low,
    int Mid,
    int High);

public record WifiCardProfilesDto(
    bool IsAvailable,
    IReadOnlyCollection<WifiCardProfileDto> Cards,
    string? Action);

public record WifiCardProfileUpdateRequest(
    string VendorId,
    string DeviceId,
    string Name,
    string? PowerMode,
    int Lowest,
    int Low,
    int Mid,
    int High);

public record HotspotSettingsDto(
    bool IsAvailable,
    int HotspotMode,
    string HotspotSsid,
    string HotspotPassword,
    string HotspotInterfaceOverride,
    string? Action)
{
    public static HotspotSettingsDto Unavailable() => new(false, 0, string.Empty, string.Empty, string.Empty, null);
}

public record HotspotSettingsUpdateRequest(
    string? Action,
    int? HotspotMode,
    string? HotspotSsid,
    string? HotspotPassword,
    string? HotspotInterfaceOverride);

public record HardwareConfigDto(
    bool IsAvailable,
    bool WifiEnableAutodetect,
    string WifiWbLinkCards,
    string WifiHotspotCard,
    bool WifiMonitorCardEmulate,
    bool WifiForceNoLinkButHotspot,
    bool WifiLocalNetworkEnable,
    string WifiLocalNetworkSsid,
    string WifiLocalNetworkPassword,
    string NwEthernetCard,
    string NwManualForwardingIps,
    bool NwForwardToLocalhost58xx,
    bool GenEnableLastKnownPosition,
    int GenRfMetricsLevel,
    string GroundUnitIp,
    string AirUnitIp,
    int VideoPort,
    int TelemetryPort,
    bool DisableMicrohardDetection,
    bool ForceMicrohard,
    string MicrohardUsername,
    string MicrohardPassword,
    string MicrohardIpAir,
    string MicrohardIpGround,
    string MicrohardIpRange,
    int MicrohardVideoPort,
    int MicrohardTelemetryPort)
{
    public static HardwareConfigDto Unavailable() => new(
        false,
        true,
        string.Empty,
        string.Empty,
        false,
        false,
        false,
        string.Empty,
        string.Empty,
        "RPI_ETHERNET_ONLY",
        string.Empty,
        false,
        false,
        0,
        string.Empty,
        string.Empty,
        5000,
        5600,
        false,
        false,
        "admin",
        "qwertz1",
        string.Empty,
        string.Empty,
        string.Empty,
        5910,
        5920);
}

public record HardwareConfigUpdateRequest(
    bool? WifiEnableAutodetect,
    string? WifiWbLinkCards,
    string? WifiHotspotCard,
    bool? WifiMonitorCardEmulate,
    bool? WifiForceNoLinkButHotspot,
    bool? WifiLocalNetworkEnable,
    string? WifiLocalNetworkSsid,
    string? WifiLocalNetworkPassword,
    string? NwEthernetCard,
    string? NwManualForwardingIps,
    bool? NwForwardToLocalhost58xx,
    bool? GenEnableLastKnownPosition,
    int? GenRfMetricsLevel,
    string? GroundUnitIp,
    string? AirUnitIp,
    int? VideoPort,
    int? TelemetryPort,
    bool? DisableMicrohardDetection,
    bool? ForceMicrohard,
    string? MicrohardUsername,
    string? MicrohardPassword,
    string? MicrohardIpAir,
    string? MicrohardIpGround,
    string? MicrohardIpRange,
    int? MicrohardVideoPort,
    int? MicrohardTelemetryPort);

public record PartitionEntryDto(
    string Device,
    string? Mountpoint,
    string? Fstype,
    string? Label,
    long? FreeBytes,
    long SizeBytes,
    long StartBytes);

public record PartitionSegmentDto(
    string Kind,
    string? Device,
    string? Mountpoint,
    string? Fstype,
    string? Label,
    long StartBytes,
    long SizeBytes);

public record PartitionDiskDto(
    string Name,
    long SizeBytes,
    IReadOnlyCollection<PartitionSegmentDto> Segments,
    IReadOnlyCollection<PartitionEntryDto> Partitions);

public record PartitionResizableDto(
    string Device,
    string? Label,
    string? Fstype,
    long FreeBytes);

public record PartitionReportDto(
    IReadOnlyCollection<PartitionDiskDto> Disks,
    RecordingInfoDto? Recordings,
    PartitionResizableDto? Resizable);

public record RecordingInfoDto(long FreeBytes, IReadOnlyCollection<string> Files);

public record PartitionResizeRequestDto(bool Resize);
