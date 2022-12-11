using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Commands;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;
    private readonly SystemCommandsService _systemCommandsService;

    public SystemController(
        ILogger<SystemController> logger,
        SystemCommandsService systemCommandsService)
    {
        _logger = logger;
        _systemCommandsService = systemCommandsService;
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
}

