using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using BackgroundWorker.SignalR;
using BackgroundWorker.Utils;
using ServiceBusUtils;
using System.Diagnostics;

namespace BackgroundWorker.HostedServices
{
    public class DeadLetterProcessingHostedService : IHostedService
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly BackgroundWorkerSettings _backgroundWorkerSettings;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger<DeadLetterProcessingHostedService> _logger;
        private readonly CancellationToken _mainCancellationToken;
        private readonly SignalRUtils _notificationHub;
        private readonly ServiceBusConsumer _serviceBusTransferDeadLetterConsumer;
        private readonly ServiceBusConsumer _serviceBusDeadLetterConsumer;
        private readonly IServiceBusSettings _serviceBusSettings;
        private readonly IServiceScopeFactory _serviceScopeFactory;
#pragma warning restore IDE0052 // Remove unread private members

        public DeadLetterProcessingHostedService(
            IServiceBusSettings serviceBusSettings,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DeadLetterProcessingHostedService> logger,
            BackgroundWorkerSettings backgroundWorkerSettings,
#pragma warning disable IDE0060 // Remove unused parameter
            BlobServiceClient blobServiceClient,
#pragma warning restore IDE0060 // Remove unused parameter
            SignalRUtils notificationHub)
        {
            _notificationHub = notificationHub;
            _backgroundWorkerSettings = backgroundWorkerSettings;

            _cancellationTokenSource = new CancellationTokenSource();
            _mainCancellationToken = _cancellationTokenSource.Token;

            _serviceBusSettings = serviceBusSettings;
            _serviceBusTransferDeadLetterConsumer = new ServiceBusConsumer(
                _serviceBusSettings,
                ReceivedMessageFunc,
                ReceivedErrorFunc,
                maxConcurrentCalls: _backgroundWorkerSettings.NumberOfHandlers,
                subQueue: SubQueue.TransferDeadLetter,
                autoCompleteMessages: true,
                _mainCancellationToken);
            _serviceBusDeadLetterConsumer = new ServiceBusConsumer(
                _serviceBusSettings,
                ReceivedMessageFunc,
                ReceivedErrorFunc,
                maxConcurrentCalls: _backgroundWorkerSettings.NumberOfHandlers,
                subQueue: SubQueue.DeadLetter,
                autoCompleteMessages: true,
                _mainCancellationToken);

            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        private Task ReceivedErrorFunc(ProcessErrorEventArgs args)
        {
            Debugger.Break();
            _logger.LogError(args.Exception, $"Received error message: {args.Exception.Message}");
            // such messages will appear in dead letter queue, handle it there
            return Task.CompletedTask;
        }

        private async Task<(Dictionary<string, object>? ModifiedProperties, bool Success)> ReceivedMessageFunc(ProcessMessageEventArgs args)
        {
            _logger.LogWarning(args.Message.DeadLetterErrorDescription);
            //Debugger.Break();
            await args.CompleteMessageAsync(args.Message);
            return default;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Consuming is starting..");

            await _serviceBusDeadLetterConsumer.StartListenAsync();
            await _serviceBusTransferDeadLetterConsumer.StartListenAsync();

            _logger.LogInformation("Consuming has been started.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Consuming is stopping..");

            try { await _serviceBusDeadLetterConsumer.DisposeAsync(); } catch { /* ignore if now */ }
            try { await _serviceBusTransferDeadLetterConsumer.DisposeAsync(); } catch { /* ignore if now */ }
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            _logger.LogInformation("Consuming has been stopped.");
        }
    }
}