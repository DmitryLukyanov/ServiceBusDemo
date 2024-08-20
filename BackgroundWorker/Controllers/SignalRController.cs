using BackgroundWorker.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BackgroundWorker.Controllers
{
    public class SignalRController(IHubContext<NotificationHub> _notificationHub, ILogger<SignalRController> _logger) : ControllerBase
    {
        [HttpPost("GenerateMessage")]
        public async Task PostMessage()
        {
            _logger.LogInformation("Sending message to SignalR");

            await _notificationHub.Clients.All.SendAsync(
                method: "OnOperationComplited",
                //index, query, createdAt, resulteUrl
                arg1: Guid.NewGuid(), // TODO: review logic
                arg2: "SELECT 1",
                arg3: DateTime.UtcNow,
                arg4: "www.resultedlink.com",
                cancellationToken: CancellationToken.None);

            _logger.LogInformation("The message to SignalR has been sent");
        }
    }
}
