using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Partitions;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/partitions")]
public class PartitionsController : ControllerBase
{
    private readonly SysutilPartitionService _service;

    public PartitionsController(SysutilPartitionService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<PartitionReportDto> Get(CancellationToken cancellationToken)
    {
        return _service.GetPartitionsAsync(cancellationToken);
    }

    [HttpPost("resize")]
    public async Task<IActionResult> RequestResize([FromBody] PartitionResizeRequestDto request,
        CancellationToken cancellationToken)
    {
        var ok = await _service.SendResizeRequestAsync(request.Resize, cancellationToken);
        return ok ? Ok() : StatusCode(503);
    }
}
