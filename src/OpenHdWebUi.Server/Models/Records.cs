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
