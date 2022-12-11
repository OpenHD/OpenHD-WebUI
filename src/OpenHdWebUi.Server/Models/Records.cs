namespace OpenHdWebUi.Server.Models;

public record SystemCommandDto(string Id, string DisplayName);
public record RunCommandRequest(string Id);