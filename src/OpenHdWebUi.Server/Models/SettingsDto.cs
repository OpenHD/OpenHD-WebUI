namespace OpenHdWebUi.Server.Models;

public record SettingFileSummaryDto(string Id, string Name, string RootDirectory, string RelativePath);

public record SettingFileContentDto(string Id, string Name, string Content);

public record UpdateSettingFileRequest(string Id, string Content);
