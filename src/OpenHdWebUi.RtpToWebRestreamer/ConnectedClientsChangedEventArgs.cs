using JetBrains.Annotations;

namespace OpenHdWebUi.RtpToWebRestreamer;

[PublicAPI]
public class ConnectedClientsChangedEventArgs : EventArgs
{
    public ConnectedClientsChangedEventArgs(int newCount)
    {
        NewCount = newCount;
    }

    public int NewCount { get; }
}
