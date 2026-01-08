using Cysharp.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.AirGround;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/air-ground")]
public class AirGroundController : ControllerBase
{
    private readonly SysutilRunModeService _runModeService;

    public AirGroundController(SysutilRunModeService runModeService)
    {
        _runModeService = runModeService;
    }

    [HttpGet]
    public async Task<ActionResult<RunModeInfoDto>> GetRunMode(CancellationToken cancellationToken)
    {
        var info = await _runModeService.GetRunModeAsync(cancellationToken);
        return Ok(info);
    }

    [HttpPost]
    public async Task<ActionResult<RunModeUpdateResponseDto>> SetRunMode(
        [FromBody] RunModeUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Mode))
        {
            return BadRequest();
        }

        var normalized = request.Mode.Trim().ToLowerInvariant();
        var update = await _runModeService.SetRunModeAsync(normalized, cancellationToken);
        if (!update.Ok)
        {
            return StatusCode(502, update);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            await ProcessX.StartAsync("systemctl restart openhd").WaitAsync(cancellationToken);
        }

        return Ok(new RunModeUpdateResponseDto(true, "OpenHD restart requested.", normalized));
    }
}
