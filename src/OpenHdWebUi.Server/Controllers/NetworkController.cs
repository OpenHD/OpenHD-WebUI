using Microsoft.AspNetCore.Mvc;
using OpenHdWebUi.Server.Models;
using OpenHdWebUi.Server.Services.Network;
using System.Threading.Tasks;

namespace OpenHdWebUi.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NetworkController : ControllerBase
{
    private readonly NetworkInfoService _service;

    public NetworkController(NetworkInfoService service)
    {
        _service = service;
    }

    [HttpGet("info")]
    public NetworkInfoDto GetInfo()
    {
        return _service.GetNetworkInfo();
    }

    [HttpPost("ethernet")]
    public async Task<IActionResult> SetEthernet([FromBody] SetIpRequest request)
    {
        await _service.SetEthernetIpAsync(request.Interface, request.Ip, request.Netmask);
        return Ok();
    }
}
