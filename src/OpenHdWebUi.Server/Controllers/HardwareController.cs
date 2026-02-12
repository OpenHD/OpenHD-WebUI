using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Hardware;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HardwareController : ControllerBase
{
    private readonly SysutilHardwareService _hardwareService;
    private readonly HotspotSettingsService _hotspotSettingsService;

    public HardwareController(SysutilHardwareService hardwareService,
        HotspotSettingsService hotspotSettingsService)
    {
        _hardwareService = hardwareService;
        _hotspotSettingsService = hotspotSettingsService;
    }

    [HttpGet("platform")]
    public Task<PlatformInfoDto> GetPlatform(CancellationToken cancellationToken)
    {
        return _hardwareService.GetPlatformAsync(cancellationToken);
    }

    [HttpPost("platform")]
    public Task<PlatformInfoDto> UpdatePlatform([FromBody] PlatformUpdateRequest request,
        CancellationToken cancellationToken)
    {
        return _hardwareService.UpdatePlatformAsync(request, cancellationToken);
    }

    [HttpGet("wifi")]
    public Task<WifiInfoDto> GetWifi(CancellationToken cancellationToken)
    {
        return _hardwareService.GetWifiAsync(cancellationToken);
    }

    [HttpPost("wifi")]
    public Task<WifiInfoDto> UpdateWifi([FromBody] WifiUpdateRequest request,
        CancellationToken cancellationToken)
    {
        return _hardwareService.UpdateWifiAsync(request, cancellationToken);
    }

    [HttpGet("hotspot")]
    public HotspotSettingsDto GetHotspot()
    {
        return _hotspotSettingsService.GetHotspotSettings();
    }

    [HttpPost("hotspot")]
    public HotspotSettingsDto UpdateHotspot([FromBody] HotspotSettingsUpdateRequest request)
    {
        return _hotspotSettingsService.UpdateHotspotSettings(request);
    }
}
