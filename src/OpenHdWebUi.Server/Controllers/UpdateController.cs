using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenHdWebUi.Server.Configuration;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Update;

namespace OpenHdWebUi.Server.Controllers;

[Route("api/update")]
[ApiController]
public class UpdateController : ControllerBase
{
    private readonly ServiceConfiguration _configuration;
    private readonly SysutilUpdateService _updateService;

    public UpdateController(
        IOptions<ServiceConfiguration> configuration,
        SysutilUpdateService updateService)
    {
        _configuration = configuration.Value;
        _updateService = updateService;
    }

    [HttpGet("info")]
    public Task<SysutilUpdateInfoDto> GetUpdateInfo(CancellationToken cancellationToken)
    {
        return _updateService.GetInfoAsync(cancellationToken);
    }

    [HttpPost("run")]
    public async Task<ActionResult<SysutilUpdateRunResponseDto>> RunUpdate(CancellationToken cancellationToken)
    {
        var result = await _updateService.RunUpdateAsync(cancellationToken);
        if (result.Accepted)
        {
            return Ok(result);
        }

        return StatusCode(502, result);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(1024 * 1024 * 1024)]
    public async Task<ActionResult> UploadFile()
    {
        var requestStream = Request.Body;
        await using var fileStream = System.IO.File.Create(_configuration.UpdateConfig.UpdateFile);
        await requestStream.CopyToAsync(fileStream);

        return NoContent();
    }
}
