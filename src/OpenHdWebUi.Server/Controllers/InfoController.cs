using System.Reflection;

using Microsoft.AspNetCore.Mvc;

using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.AirGround;

namespace OpenHdWebUi.Server.Controllers;

[Route("api/info")]
[ApiController]
public class InfoController : ControllerBase
{
    private readonly AirGroundService _airGroundService;

    public InfoController(AirGroundService airGroundService)
    {
        _airGroundService = airGroundService;
    }

    [HttpGet("ag-state")]
    public AirGroundStatus GetAirGround()
    {
        return new AirGroundStatus(_airGroundService.IsAirMode, _airGroundService.IsGroundMode);
    }

    [HttpGet("web-ui-version")]
    public string GetWebUiVersion()
    {
        var versionAttribute = GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return versionAttribute?.InformationalVersion ?? "unknown";
    }
}