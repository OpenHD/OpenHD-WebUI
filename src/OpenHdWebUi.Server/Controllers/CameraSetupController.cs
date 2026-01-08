using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Camera;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/camera-setup")]
public class CameraSetupController : ControllerBase
{
    private readonly SysutilCameraService _cameraService;

    public CameraSetupController(SysutilCameraService cameraService)
    {
        _cameraService = cameraService;
    }

    [HttpGet]
    public async Task<ActionResult<SysutilCameraInfoDto>> GetCameraInfo(CancellationToken cancellationToken)
    {
        var info = await _cameraService.GetCameraInfoAsync(cancellationToken);
        return Ok(info);
    }

    [HttpPost]
    public async Task<ActionResult<CameraSetupResponseDto>> ApplyCameraSetup(
        [FromBody] CameraSetupRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest();
        }

        var result = await _cameraService.ApplyCameraSetupAsync(request.CameraType, cancellationToken);
        if (!result.Ok)
        {
            return StatusCode(502, result);
        }

        return Ok(result);
    }
}
