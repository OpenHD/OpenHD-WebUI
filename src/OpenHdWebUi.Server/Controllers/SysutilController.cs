using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Sysutil;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/sysutil")]
public class SysutilController : ControllerBase
{
    private readonly SysutilControlService _service;

    public SysutilController(SysutilControlService service)
    {
        _service = service;
    }

    [HttpGet("debug")]
    public Task<SysutilDebugDto> GetDebug(CancellationToken cancellationToken)
    {
        return _service.GetDebugAsync(cancellationToken);
    }

    [HttpPost("debug")]
    public async Task<ActionResult<SysutilDebugUpdateResponseDto>> UpdateDebug(
        [FromBody] SysutilDebugUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _service.SetDebugAsync(request.Debug, cancellationToken);
        if (!result.Ok)
        {
            return StatusCode(502, result);
        }
        return Ok(result);
    }

    [HttpGet("platform")]
    public Task<SysutilPlatformInfoDto> GetPlatform(CancellationToken cancellationToken)
    {
        return _service.GetPlatformAsync(cancellationToken);
    }

    [HttpPost("platform/refresh")]
    public async Task<ActionResult<SysutilPlatformUpdateResponseDto>> RefreshPlatform(CancellationToken cancellationToken)
    {
        var result = await _service.UpdatePlatformAsync(
            new SysutilPlatformUpdateRequestDto("refresh", null, null),
            cancellationToken);
        if (!result.Ok)
        {
            return StatusCode(502, result);
        }
        return Ok(result);
    }

    [HttpPost("platform/override")]
    public async Task<ActionResult<SysutilPlatformUpdateResponseDto>> SetPlatformOverride(
        [FromBody] SysutilPlatformUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!request.PlatformType.HasValue)
        {
            return BadRequest(new SysutilPlatformUpdateResponseDto(false, 0, "unknown", "platformType is required."));
        }
        var result = await _service.UpdatePlatformAsync(
            new SysutilPlatformUpdateRequestDto("set", request.PlatformType, request.PlatformName),
            cancellationToken);
        if (!result.Ok)
        {
            return StatusCode(502, result);
        }
        return Ok(result);
    }

    [HttpPost("platform/clear")]
    public async Task<ActionResult<SysutilPlatformUpdateResponseDto>> ClearPlatformOverride(CancellationToken cancellationToken)
    {
        var result = await _service.UpdatePlatformAsync(
            new SysutilPlatformUpdateRequestDto("clear", null, null),
            cancellationToken);
        if (!result.Ok)
        {
            return StatusCode(502, result);
        }
        return Ok(result);
    }

    [HttpPost("video/restart")]
    public async Task<ActionResult<SysutilVideoResponseDto>> RestartVideo(CancellationToken cancellationToken)
    {
        var result = await _service.RestartVideoAsync(cancellationToken);
        if (!result.Ok)
        {
            return StatusCode(502, result);
        }
        return Ok(result);
    }
}
