using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Status;

namespace OpenHdWebUi.Server.Controllers;

[Route("api/status")]
[ApiController]
public class StatusController : ControllerBase
{
    private readonly SysutilStatusService _statusService;

    public StatusController(SysutilStatusService statusService)
    {
        _statusService = statusService;
    }

    [HttpGet]
    public Task<OpenHdStatusDto> GetStatus(CancellationToken cancellationToken)
    {
        return _statusService.GetStatusAsync(cancellationToken);
    }
}
