using Microsoft.AspNetCore.SignalR;

namespace BackgroundWorker.SignalR
{
    public class SignalRUtils(IHubContext<NotificationHub> _notificationHub)
    {
        private const string UnauthorizedUserName = "unauthorized"; // TODO: should not happen in real app, remove as soon as auth configured

        public static async Task ConfigureOnConnectedAsync(Hub hub, CancellationToken cancellationToken = default)
        {
            var context = hub.Context;
            var groups = hub.Groups;

            // some details can be found here https://consultwithgriff.com/signalr-connection-ids/
            var userName = context!.User!.Identity!.Name
                ?? UnauthorizedUserName;

            var userGroupName = CreateUserGroupName(userName);
            await groups.AddToGroupAsync(context.ConnectionId, userGroupName, cancellationToken);
        }

        public async Task SendAsync(
            string javascriptMethodName,
            string? userName,
            object? arg1,
            object? arg2,
            object? arg3,
            object? arg4,
            CancellationToken cancellationToken)
        {
            var userGroupName = CreateUserGroupName(userName ?? UnauthorizedUserName);
            var group = _notificationHub.Clients.Group(userGroupName);
            await group.SendAsync(
                method: javascriptMethodName,
                arg1: arg1,
                arg2: arg2,
                arg3: arg3,
                arg4: arg4,
                cancellationToken: cancellationToken);
        }

        private static string CreateUserGroupName(string userName) => $"user_{userName}";
    }
}
