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
        var fullPath = _mediaService.MediaDirectoryFullPath;

        var files = _mediaService.GetMediaFilesPaths();

        var serverFileInfos = new List<ServerFileInfo>();
        foreach (var fileName in files)
        {
            var fileInfo = new FileInfo(fileName);
            var relativeToFilesDir = Path.GetRelativePath(fullPath, fileInfo.FullName);
            var serverFile = new ServerFileInfo(
                fileInfo.Name,
                Flurl.Url.Combine("media", relativeToFilesDir)
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

