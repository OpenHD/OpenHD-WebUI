using System.Net;
using Bld.RtpReceiver;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using WebSocketSharp.Server;

namespace OpenHdWebUi.Server.Services.Fpv;

public class WebRtcHandler
{
    private readonly ILogger<WebRtcHandler> _logger;
    private readonly WebSocketServer _webSocketServer;
    private const string STUN_URL = "stun:stun.sipsorcery.com";
    private const int WEBSOCKET_PORT = 8081;
    private const int VIDEO_INITIAL_WIDTH = 640;
    private const int VIDEO_INITIAL_HEIGHT = 480;

    private readonly Receiver _receiver;

    public WebRtcHandler(ILogger<WebRtcHandler> logger, ILoggerFactory factory)
    {
        _logger = logger;
        _webSocketServer = new WebSocketServer(IPAddress.Any, WEBSOCKET_PORT);
        _webSocketServer.AddWebSocketService<WebRTCWebSocketPeer>("/", (peer) => peer.CreatePeerConnection = CreatePeerConnection);
        _receiver = new Receiver(new IPEndPoint(IPAddress.Any, 5600), factory.CreateLogger<Receiver>());
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

        // WindowsVideoEndPoint winVideoEP = new WindowsVideoEndPoint(new VpxVideoEncoder(), WEBCAM_NAME);

        //bool initResult = await winVideoEP.InitialiseVideoSourceDevice();
        //if (!initResult)
        //{
        //    throw new ApplicationException("Could not initialise video capture device.");
        //}
        MediaStreamTrack videoTrack = new MediaStreamTrack(new VideoFormat(VideoCodecsEnum.H264, 96), MediaStreamStatusEnum.SendRecv);
        pc.addTrack(videoTrack);

        _receiver.OnVideoFrameReceivedByIndex += (i, point, arg3, arg4) => pc.SendVideo(1, arg4) ;
        //pc.OnVideoFormatsNegotiated += (videoFormats) =>
        //     winVideoEP.SetVideoSourceFormat(videoFormats.First());

        pc.onconnectionstatechange += async (state) =>
        {
            _logger.LogDebug("Peer connection state change to {state}.", state);

            if (state == RTCPeerConnectionState.connected)
            {
                //await winVideoEP.StartVideo();
            }
            else if (state == RTCPeerConnectionState.failed)
            {
                pc.Close("ice disconnection");
            }
            else if (state == RTCPeerConnectionState.closed)
            {
                //await winVideoEP.CloseVideo();
            }
        };

        // Diagnostics.
        pc.OnReceiveReport += (re, media, rr) => _logger.LogDebug($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
        pc.OnSendReport += (media, sr) => _logger.LogDebug($"RTCP Send for {media}\n{sr.GetDebugSummary()}");
        pc.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => _logger.LogDebug($"STUN {msg.Header.MessageType} received from {ep}.");
        pc.oniceconnectionstatechange += (state) => _logger.LogDebug($"ICE connection state change to {state}.");

        return pc;
    }
}