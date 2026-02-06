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
