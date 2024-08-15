using BackgroundWorker.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BackgroundWorker.Controllers
{
    public class SignalRController(IHubContext<NotificationHub> notificationHub, ILogger<SignalRController> logger) : ControllerBase
    {
        // POC related
        private const string NoImagePictureLink = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR1ayLa8IDnr7QM3REMYlfPL0uDLWjrAt3eKw&s";
        
        private readonly IHubContext<NotificationHub> _notificationHub = notificationHub;
        private readonly ILogger<SignalRController> _logger = logger;

        [HttpPost("GenerateNotificationMessage")]
        public async Task PostMessage()
        {
            _logger.LogInformation("Sending message to SignalR");

            await _notificationHub.Clients.All.SendAsync(
                method: "OnOperationNotified",
                //index, query, createdAt, resulteUrl
                arg1: Guid.NewGuid(), // TODO: review logic
                arg2: "SELECT 1",
                arg3: DateTime.UtcNow,
                arg4: NoImagePictureLink,
                arg5: TimeSpan.FromSeconds(2),
                cancellationToken: CancellationToken.None);

            _logger.LogInformation("The message to SignalR has been sent");
        }
    }
}
