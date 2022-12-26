using Microsoft.Extensions.Options;
using OpenHdWebUi.FFmpeg;
using OpenHdWebUi.Server.Configuration;

namespace OpenHdWebUi.Server.Services.Media;

public class MediaService
{
    private readonly object _lock = new();
    private bool _isInProgress;

    public MediaService(IOptions<ServiceConfiguration> configuration)
    {
        MediaDirectoryFullPath = Path.GetFullPath(configuration.Value.FilesFolder);
    }

    public bool IsInProgress => _isInProgress;

    public string MediaDirectoryFullPath { get; }

    public string PreviewsDirectoryFullPath => MediaConsts.PreviewsFsPath;

    public string[] GetMediaFilesPaths()
    {
        return Directory.GetFiles(MediaDirectoryFullPath, "*.mkv");
    }

    public async Task StartPreviewsCreationAsync()
    {
        lock (_lock)
        {
            if (_isInProgress)
            {
                return;
            }

            _isInProgress = true;
        }

        var files = GetMediaFilesPaths();

        await Parallel.ForEachAsync(files, async (file, token) =>
        {
            var creator = new PreviewCreator(file, PreviewsDirectoryFullPath);
            await creator.StartAsync();
        });

        lock (_lock)
        {
            _isInProgress = false;
        }
    }
}