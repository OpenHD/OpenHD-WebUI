using SIPSorcery.Net;

namespace OpenHdWebUi.RtpToWebRestreamer;

internal class MultiplexedPeer
{
    private readonly RTCPeerConnection _peer;
    private bool _isStarted;

    public MultiplexedPeer(RTCPeerConnection peer)
    {
        _peer = peer;
    }

    public void SendVideo(byte[] sample)
    {
        if (!_isStarted)
        {
            return;
        }

        _peer.SendVideo(1, sample);
    }

    public void Start()
    {
        _isStarted = true;
    }

    public void Stop()
    {
        _isStarted = false;
    }
}