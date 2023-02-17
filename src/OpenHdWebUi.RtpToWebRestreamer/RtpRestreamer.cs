using System.Net;
using Bld.RtpReceiver;

using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;

using SIPSorceryMedia.Abstractions;

using WebSocketSharp.Server;

namespace OpenHdWebUi.RtpToWebRestreamer;

[PublicAPI]
public class RtpRestreamer
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<RtpRestreamer> _logger;
    private readonly WebSocketServer _webSocketServer;
    private readonly Receiver _receiver;
    private readonly StreamMultiplexer _streamMultiplexer;

    public RtpRestreamer(
        IPEndPoint webSocketEndpoint,
        IPEndPoint rtpListenEndpoint,
        ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<RtpRestreamer>();

        _webSocketServer = new WebSocketServer(webSocketEndpoint.Address, webSocketEndpoint.Port, false);
        _webSocketServer.AddWebSocketService<WebRTCWebSocketPeer>("/", (peer) => peer.CreatePeerConnection = CreatePeerConnection);

        _receiver = new Receiver(rtpListenEndpoint, loggerFactory.CreateLogger<Receiver>());
        _streamMultiplexer = new StreamMultiplexer(_receiver, _loggerFactory.CreateLogger<StreamMultiplexer>());
    }

    public void Start()
    {
        _webSocketServer.Start();
        _receiver.Start();
    }

    private async Task<RTCPeerConnection> CreatePeerConnection()
    {
        RTCConfiguration config = new RTCConfiguration();
        var pc = new RTCPeerConnection(config);
        _streamMultiplexer.RegisterPeer(pc);

        var videoTrack = new MediaStreamTrack(
            new VideoFormat(VideoCodecsEnum.H264, 96),
            MediaStreamStatusEnum.SendRecv);
        pc.addTrack(videoTrack);

        pc.onconnectionstatechange += (state) =>
        {
            _logger.LogDebug("Peer connection state change to {state}.", state);

            if (state == RTCPeerConnectionState.connected)
            {
                _streamMultiplexer.StartPeerTransmit(pc);
            }
            else if (state == RTCPeerConnectionState.failed)
            {
                _streamMultiplexer.StopPeerTransmit(pc);
                pc.Close("ice disconnection");
            }
            else if (state == RTCPeerConnectionState.closed)
            {
                _streamMultiplexer.StopPeerTransmit(pc);
            }
        };

        // Diagnostics.
        pc.OnReceiveReport += (re, media, rr) =>
        {
            _logger.LogDebug($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
        };
        pc.OnSendReport += (media, sr) =>
        {
            _logger.LogDebug($"RTCP Send for {media}\n{sr.GetDebugSummary()}");
        };
        pc.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) =>
        {
            _logger.LogDebug($"STUN {msg.Header.MessageType} received from {ep}.");
        };
        pc.oniceconnectionstatechange += (state) =>
        {
            _logger.LogDebug("ICE connection state change to {state}.", state);
        };

        return pc;
    }
}
