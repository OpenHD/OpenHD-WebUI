namespace OpenHdWebUi.Server.Models;

public record SystemCommandDto(string Id, string DisplayName);

public record SystemFileDto(string Id, string DisplayName);

public record RunCommandRequest(string Id);

public record RunTerminalCommandRequest(string Command);

public record ServerFileInfo(string FileName, string DownloadPath);

public record AirGroundStatus(bool IsAir, bool IsGround);

public record LoginRequest(string Username, string Password);