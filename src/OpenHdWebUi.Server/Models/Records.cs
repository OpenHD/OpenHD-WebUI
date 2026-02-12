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
    bool Disabled);

public record WifiInfoDto(
    bool IsAvailable,
    IReadOnlyCollection<WifiCardInfoDto> Cards,
    string? Action)
{
    public static WifiInfoDto Unavailable() => new(false, Array.Empty<WifiCardInfoDto>(), null);
}

public record WifiUpdateRequest(string Action, string? Interface, string? OverrideType);

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
