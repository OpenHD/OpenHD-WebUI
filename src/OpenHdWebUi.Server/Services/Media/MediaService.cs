using Microsoft.Extensions.Options;
using OpenHdWebUi.FFmpeg;
using OpenHdWebUi.Server.Configuration;

namespace OpenHdWebUi.Server.Services.Media;

public class MediaService
{
    private readonly ILogger<MediaService> _logger;
    private readonly object _lock = new();
    private bool _isInProgress;
    private Task? _conversionTask;

    public MediaService(
        IOptions<ServiceConfiguration> configuration,
        ILogger<MediaService> logger)
    {
        _logger = logger;
        MediaDirectoryFullPath = Path.GetFullPath(configuration.Value.FilesFolder);
    }

    public bool IsInProgress => _isInProgress;

    public string MediaDirectoryFullPath { get; }

    public string PreviewsDirectoryFullPath => MediaConsts.PreviewsFsPath;

    public string[] GetMediaFilesPaths()
    {
        return Directory.GetFiles(MediaDirectoryFullPath, "*.mkv");
    }

    public void DeleteFile(string fileName)
    {
        var fullFilePath = Path.Combine(MediaDirectoryFullPath, fileName);
        if (Path.Exists(fullFilePath))
        {
            File.Delete(fullFilePath);
        }
    }

    public void StartPreviewsCreation()
    {
        lock (_lock)
        {
            if (_isInProgress)
            {
                return;
            }

            _isInProgress = true;
        }

        _conversionTask = StartPreviewsCreationInternalAsync();

        lock (_lock)
        {
            _isInProgress = false;
        }
    }

    private async Task StartPreviewsCreationInternalAsync()
    {
        _logger.LogInformation("Starting previews creation");
        try
        {
            var files = GetMediaFilesPaths();

            await Parallel.ForEachAsync(files, async (file, token) =>
            {
                var creator = new PreviewCreator(file, PreviewsDirectoryFullPath);
                await creator.StartAsync();
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while preview creation");
            lock (_lock)
            {
                _isInProgress = false;
            }
        }
    }
}