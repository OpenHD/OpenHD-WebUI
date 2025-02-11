using Microsoft.AspNetCore.Mvc;

namespace OpenHdWebUi.Server.Controllers;

[Route("api/update")]
[ApiController]
public class UpdateController: ControllerBase
{
    [HttpPost("upload")]
    public async Task<ActionResult> UploadFile()
    {
        var requestStream = Request.Body;
        await using var fileStream = System.IO.File.Create("update.zip");
        await requestStream.CopyToAsync(fileStream);

        return NoContent();
    }
}