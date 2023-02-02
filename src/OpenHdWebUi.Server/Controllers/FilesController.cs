using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Media;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly ILogger<FilesController> _logger;
    private readonly IOptions<ServiceConfiguration> _configuration;
    private readonly MediaService _mediaService;

    public FilesController(
        ILogger<FilesController> logger,
        IOptions<ServiceConfiguration> configuration,
        MediaService mediaService)
    {
        _logger = logger;
        _configuration = configuration;
        _mediaService = mediaService;
    }

    [HttpGet]
    public async Task<IEnumerable<ServerFileInfo>> Get()
    {
        _mediaService.StartPreviewsCreation();

        // Wait some time for attempt of sync previews creation
        // Should works for small amount of videos
        await Task.Delay(TimeSpan.FromSeconds(0.5));
        for (int i = 0; i < 10; i++)
        {
            if (!_mediaService.IsInProgress)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        var fullPath = _mediaService.MediaDirectoryFullPath;

        var files = _mediaService.GetMediaFilesPaths();

        var serverFileInfos = new List<ServerFileInfo>();
        foreach (var fileName in files)
        {
            var fileInfo = new FileInfo(fileName);
            var relativeToFilesDir = Path.GetRelativePath(fullPath, fileInfo.FullName);
            var serverFile = new ServerFileInfo(
                fileInfo.Name,
                Flurl.Url.Combine("media", relativeToFilesDir),
                Flurl.Url.Combine(
                    MediaConsts.PreviewsWebPath,
                    Path.GetDirectoryName(relativeToFilesDir),
                    $"{Path.GetFileNameWithoutExtension(relativeToFilesDir)}.webp")
            );
            serverFileInfos.Add(serverFile);
        }

        return serverFileInfos;
    }

    [HttpDelete("{fileName}")]
    public async Task Delete(string fileName)
    {
        _mediaService.DeleteFile(fileName);
    }
}

