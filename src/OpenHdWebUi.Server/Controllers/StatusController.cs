using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Status;

namespace OpenHdWebUi.Server.Controllers;

[Route("api/status")]
[ApiController]
public class StatusController : ControllerBase
{
    private readonly SysutilStatusService _statusService;
    private readonly SysutilRfControlService _rfControlService;

    public StatusController(SysutilStatusService statusService, SysutilRfControlService rfControlService)
    {
        _statusService = statusService;
        _rfControlService = rfControlService;
    }

    [HttpGet]
    public Task<OpenHdStatusDto> GetStatus(CancellationToken cancellationToken)
    {
        return _statusService.GetStatusAsync(cancellationToken);
    }

    [HttpGet("stream")]
    public Task<OpenHdStatusDto> GetStatusStream([FromQuery] long since, CancellationToken cancellationToken)
    {
        return _statusService.WaitForStatusChangeAsync(since, cancellationToken);
    }

    [HttpPost("rf-control")]
    public async Task<ActionResult<RfControlResponse>> ApplyRfControl([FromBody] RfControlRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest();
        }

        var response = await _rfControlService.ApplyAsync(request, cancellationToken);
        return Ok(response);
    }
}
