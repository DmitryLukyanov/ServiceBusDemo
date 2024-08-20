using Microsoft.AspNetCore.SignalR;
using System.Threading;

namespace BackgroundWorker.SignalR
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userName = Context!.User!.Identity!.Name ?? "Empty";

            var userGroupName = CreateUserGroupName(userName);
            await Groups.AddToGroupAsync(Context.ConnectionId, userGroupName);

            await base.OnConnectedAsync();
        }

        private static string CreateUserGroupName(string userName) => $"user_{userName}";
    }
}
