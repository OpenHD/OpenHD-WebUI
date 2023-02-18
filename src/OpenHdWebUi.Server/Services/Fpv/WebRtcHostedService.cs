using System.Net;
using OpenHdWebUi.RtpToWebRestreamer;
using OpenHdWebUi.Server.Services.AirGround;

namespace OpenHdWebUi.Server.Services.Fpv;

public class WebRtcHostedService : IHostedService
{
    private readonly RtpRestreamer? _rtpRestreamer;

    public WebRtcHostedService(
        AirGroundService airGroundService,
        ILoggerFactory loggerFactory)
    {
        if (airGroundService.IsGroundMode)
        {
            _rtpRestreamer = new RtpRestreamer(
                new IPEndPoint(IPAddress.Any, 8081),
                new IPEndPoint(IPAddress.Any, 8600),
                loggerFactory
            );
            _rtpRestreamer.ConnectedClientsChanged += RtpRestreamerOnConnectedClientsChanged;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _rtpRestreamer?.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void RtpRestreamerOnConnectedClientsChanged(object? sender, ConnectedClientsChangedEventArgs e)
    {
        if (e.NewCount == 0)
        {
            // Ensure port mirror stopped
        }
        else
        {
            // Ensure port mirror started
        }
    }
}