using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using OpenHdWebUi.Server.Configuration;

namespace OpenHdWebUi.Server.Controllers;

[Route("api/update")]
[ApiController]
public class UpdateController: ControllerBase
{
    private readonly ServiceConfiguration _configuration;

    public UpdateController(IOptions<ServiceConfiguration> configuration)
    {
        _configuration = configuration.Value;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(1024*1024*1024)]
    public async Task<ActionResult> UploadFile()
    {
        var requestStream = Request.Body;
        await using var fileStream = System.IO.File.Create(_configuration.UpdateConfig.UpdateFile);
        await requestStream.CopyToAsync(fileStream);

        return NoContent();
    }
}