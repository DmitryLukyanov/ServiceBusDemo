using Microsoft.AspNetCore.SignalR;

namespace BackgroundWorker.SignalR
{
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }
    }
}
