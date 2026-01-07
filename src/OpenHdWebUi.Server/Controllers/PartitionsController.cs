using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Partitions;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/partitions")]
public class PartitionsController : ControllerBase
{
    private readonly PartitionService _service;

    public PartitionsController(PartitionService service)
    {
        _service = service;
    }

    [HttpGet]
    public PartitionReportDto Get()
    {
        return _service.GetPartitions();
    }
}
