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
    private readonly WifiCardProfileService _wifiCardProfileService;

    public HardwareController(SysutilHardwareService hardwareService,
        HotspotSettingsService hotspotSettingsService,
        WifiCardProfileService wifiCardProfileService)
    {
        _hardwareService = hardwareService;
        _hotspotSettingsService = hotspotSettingsService;
        _wifiCardProfileService = wifiCardProfileService;
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

    [HttpGet("config")]
    public Task<HardwareConfigDto> GetHardwareConfig(CancellationToken cancellationToken)
    {
        return _hardwareService.GetHardwareConfigAsync(cancellationToken);
    }

    [HttpPost("config")]
    public Task<HardwareConfigDto> UpdateHardwareConfig([FromBody] HardwareConfigUpdateRequest request,
        CancellationToken cancellationToken)
    {
        return _hardwareService.UpdateHardwareConfigAsync(request, cancellationToken);
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

    [HttpGet("wifi-profiles")]
    public WifiCardProfilesDto GetWifiProfiles()
    {
        return _wifiCardProfileService.GetProfiles();
    }

    [HttpPost("wifi-profiles")]
    public WifiCardProfilesDto UpdateWifiProfiles([FromBody] WifiCardProfileUpdateRequest request)
    {
        return _wifiCardProfileService.UpdateProfile(request);
    }
}
