using System.Net;
using System.Net.Sockets;
using OpenHdWebUi.Server.Services.AirGround;

namespace OpenHdWebUi.Server.Services;

public class UdpProxy
{
    private readonly ILogger<UdpProxy> _logger;
    private readonly bool _isAvailable;

    private readonly UdpClient _listenClient;
    private readonly Task _listenTask;

    public UdpProxy(
        int listenPort,
        AirGroundService airGroundService,
        ILogger<UdpProxy> logger)
    {
        _logger = logger;
        _isAvailable = airGroundService.IsGroundMode;
        if (!_isAvailable)
        {
            return;
        }

        _listenClient = new UdpClient(new IPEndPoint(IPAddress.Any, listenPort));
        _listenTask = ListenPortAsync();
    }

    private async Task ListenPortAsync()
    {
        try
        {
            var received = await _listenClient.ReceiveAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Smth wrong");
        }
    }
}