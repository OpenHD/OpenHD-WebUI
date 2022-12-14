using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Commands;
using OpenHdWebUi.Server.Services.Files;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;
    private readonly SystemCommandsService _systemCommandsService;
    private readonly SystemFilesService _systemFilesService;

    public SystemController(
        ILogger<SystemController> logger,
        SystemCommandsService systemCommandsService,
        SystemFilesService systemFilesService)
    {
        _logger = logger;
        _systemCommandsService = systemCommandsService;
        _systemFilesService = systemFilesService;
    }

    [HttpGet("get-commands")]
    public IReadOnlyCollection<SystemCommandDto> GetCommands()
    {
        return _systemCommandsService.GetAllCommands()
            .Select(c => new SystemCommandDto(c.Id, c.DisplayName))
            .ToArray();
    }

    [HttpPost("run-command")]
    public async Task RunCommand([FromBody] RunCommandRequest request)
    {
        await _systemCommandsService.TryRunCommandAsync(request.Id);
    }

    [HttpGet("get-files")]
    public IReadOnlyCollection<SystemFileDto> GetFiles()
    {
        return _systemFilesService.GetAllCommands()
            .Select(c => new SystemFileDto(c.Id, c.DisplayName))
            .ToArray();
    }

    [HttpGet("get-file/{id}")]
    public async Task<IActionResult> RunCommand([FromRoute]string id)
    {
        var (found, content) = await _systemFilesService.TryGetFileContentAsync(id);
        if (found)
        {
            return File(content!, MediaTypeNames.Text.Plain, $"{id}.txt");
        }

        return NoContent();
    }
}

