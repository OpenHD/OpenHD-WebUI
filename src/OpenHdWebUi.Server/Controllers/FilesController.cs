using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Shared;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly ILogger<FilesController> _logger;
    private readonly IOptions<ServiceConfiguration> _configuration;

    public FilesController(
        ILogger<FilesController> logger,
        IOptions<ServiceConfiguration> configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public IEnumerable<ServerFileInfo> Get()
    {
        var fullPath = Path.GetFullPath(_configuration.Value.FilesFolder);
        if (!Path.Exists(fullPath))
        {
            return Array.Empty<ServerFileInfo>();
        }

        var files = Directory.GetFiles(fullPath);

        var serverFileInfos = new List<ServerFileInfo>();
        foreach (var fileName in files)
        {
            var fileInfo = new FileInfo(fileName);
            var relativeToFilesDir = Path.GetRelativePath(fullPath, fileInfo.FullName);

            var serverFile = new ServerFileInfo()
            {
                FileName = fileInfo.Name,
                DownloadPath = $"/Media/{relativeToFilesDir}"
            };
            serverFileInfos.Add(serverFile);
        }

        return serverFileInfos;
    }
}

