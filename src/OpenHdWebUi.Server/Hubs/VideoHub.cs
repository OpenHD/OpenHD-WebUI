using Microsoft.AspNetCore.SignalR;

namespace OpenHdWebUi.Server.Hubs;

public class VideoHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}