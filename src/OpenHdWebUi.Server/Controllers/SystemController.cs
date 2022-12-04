using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Services;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;
    private readonly SystemControlService _systemControlService;

    public SystemController(
        ILogger<SystemController> logger,
        SystemControlService systemControlService)
    {
        _logger = logger;
        _systemControlService = systemControlService;
    }

    [HttpPost("restart-openhd")]
    public void RestartOpenHD()
    {
        _systemControlService.RestartOpenHd();
    }

    [HttpPost("restart-qopenhd")]
    public void RestartQOpenHD()
    {
        _systemControlService.RestartQOpenHd();
    }

    [HttpPost("reboot-system")]
    public void RebootSystem()
    {
        _systemControlService.RebootSystem();
    }

    [HttpPost("shutdown-system")]
    public void ShutdownSystem()
    {
        _systemControlService.ShutdownSystem();
    }
}

