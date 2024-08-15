using API.Data;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using ServiceBusUtils;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ServiceBusController(
        ServiceBusPublisher _serviceBusPublisher,
        IServiceBusSettings _serviceBusSettings,
        ILogger<ServiceBusController> _logger) : ControllerBase
    {
        [HttpPost("GenerateMessages")]
        public async Task PostMessage([FromQuery] int numberOfRecords = 10)
        {
            _logger.LogInformation("Generating records in queue...");

            int maxDegreeOfParallelism = 100;

            await Parallel.ForEachAsync(
                source: Enumerable.Range(0, numberOfRecords),
                parallelOptions: new ParallelOptions 
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism
                },
                body: async (index, ct) =>
                {
                    var guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                    var model = new LongRunningOperationRequestModel(id: index, query: $"SELECT '{guid}' as 'Value'", DateTime.UtcNow); // emulate query for now
                    await _serviceBusPublisher.SendAsync(model, ct);
                });

            _logger.LogInformation("Generating records in queue has been finished.");
        }

        [HttpGet("PickMessagesInQueue")]
        public async Task<IEnumerable<ServiceBusReceivedMessage>> GetMessagesInQueue([FromQuery] int maxMessages = 50)
        {
            _logger.LogInformation("Picking messages from queue...");

            await using var client = ServiceBusFactory.Create(_serviceBusSettings);
            await using var receiver = client.CreateReceiver(_serviceBusSettings.QueueName);

            var messages = await receiver.PeekMessagesAsync(maxMessages: maxMessages);
            
            _logger.LogInformation("Message from queue are shown.");

            return messages;
        }

        [HttpGet("ReceiveAndDeleteMessagesFromQueue")]
        public async Task<IEnumerable<ServiceBusReceivedMessage>> ReceiveAndDeleteMessagesFromQueue([FromQuery] int maxMessages = 50)
        {
            _logger.LogInformation("Receiving messages from queue...");

            await using var client = ServiceBusFactory.Create(_serviceBusSettings);
            await using var receiver = client.CreateReceiver(
                _serviceBusSettings.QueueName,
                new ServiceBusReceiverOptions
                {
                    ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
                });

            var messages = await receiver.ReceiveMessagesAsync(
                maxMessages: maxMessages, 
                maxWaitTime: TimeSpan.FromSeconds(5));

            _logger.LogInformation("Received message from queue are shown.");

            return messages;
        }
    }
}
