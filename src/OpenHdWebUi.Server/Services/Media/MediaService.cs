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
        MediaDirectoryFullPath = Path.GetFullPath(configuration.Value.FilesFolder);
    }

    public string MediaDirectoryFullPath { get; }

    public string[] GetMediaFilesPaths()
    {
        return Directory.GetFiles(MediaDirectoryFullPath, "*.mkv")
            .Concat(Directory.GetFiles(MediaDirectoryFullPath, "*.mp4"))
            .Concat(Directory.GetFiles(MediaDirectoryFullPath, "*.avi"))
            .ToArray();
    }

    public void DeleteFile(string fileName)
    {
        var fullFilePath = Path.Combine(MediaDirectoryFullPath, fileName);
        if (Path.Exists(fullFilePath))
        {
            File.Delete(fullFilePath);
        }
    }
}