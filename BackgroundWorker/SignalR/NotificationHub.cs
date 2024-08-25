using Microsoft.AspNetCore.SignalR;

namespace BackgroundWorker.SignalR
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await SignalRUtils.ConfigureOnConnectedAsync(this);

            await base.OnConnectedAsync();
        }
    }
}
