using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;

namespace OpenHdWebUi.Server.Services.Media;

public class MediaService
{
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IOptions<ServiceConfiguration> configuration,
        ILogger<MediaService> logger)
    {
        _logger = logger;
        MediaDirectoryFullPath = GetVideoFolderPath(configuration.Value.FilesFolders);
    }

    public string? MediaDirectoryFullPath { get; }

    public string[] GetMediaFilesPaths()
    {
        if (MediaDirectoryFullPath == null)
        {
            return [];
        }

        return Directory.GetFiles(MediaDirectoryFullPath, "*.mkv")
            .Concat(Directory.GetFiles(MediaDirectoryFullPath, "*.mp4"))
            .Concat(Directory.GetFiles(MediaDirectoryFullPath, "*.avi"))
            .ToArray();
    }

    public void DeleteFile(string fileName)
    {
        _logger.LogInformation("Deleting file {FileName}", fileName);
        if (MediaDirectoryFullPath == null)
        {
            return;
        }

        var fullFilePath = Path.Combine(MediaDirectoryFullPath, fileName);
        if (Path.Exists(fullFilePath))
        {
            File.Delete(fullFilePath);
        }
    }

    private static string? GetVideoFolderPath(List<string> configFilesFolder)
    {
        foreach (var path in configFilesFolder)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}