using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BackgroundWorker.Data;
using BackgroundWorker.Repositories;
using BackgroundWorker.SignalR;
using BackgroundWorker.Utils;
using Microsoft.AspNetCore.SignalR;
using ServiceBusUtils;
using System.Globalization;
using System.Text.Json;

namespace BackgroundWorker.HostedServices
{
    public class ServiceBusHostedService : IHostedService
    {
        private readonly BackgroundWorkerSettings _backgroundWorkerSettings;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _blobContainer;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger<ServiceBusHostedService> _logger;
        private readonly CancellationToken _mainCancellationToken;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly ServiceBusConsumer _serviceBusConsumer;
        private readonly IServiceBusSettings _serviceBusSettings;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ServiceBusHostedService(
            IServiceBusSettings serviceBusSettings,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ServiceBusHostedService> logger,
            BackgroundWorkerSettings backgroundWorkerSettings,
            BlobServiceClient blobServiceClient,
            IHubContext<NotificationHub> notificationHub)
        {
            _notificationHub = notificationHub;
            _backgroundWorkerSettings = backgroundWorkerSettings;
            _blobServiceClient = blobServiceClient;
            _blobContainer = _blobServiceClient.GetBlobContainerClient(_backgroundWorkerSettings.BlobContainerName);

            _cancellationTokenSource = new CancellationTokenSource();
            _mainCancellationToken = _cancellationTokenSource.Token;

            _serviceBusSettings = serviceBusSettings;
            _serviceBusConsumer = new ServiceBusConsumer(
                _serviceBusSettings,
                ReceivedMessageFunc,
                ReceivedErrorFunc,
                maxConcurrentCalls: _backgroundWorkerSettings.NumberOfHandlers,
                subQueue: default,
                _mainCancellationToken);
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        private Task ReceivedErrorFunc(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, $"Received error message: {args.Exception.Message}");
            // TODO: signalr about error
            // TODO2: error handling
            throw new NotImplementedException();
        }

        private async Task<(Dictionary<string, object>? ModifiedProperties, bool Success)> ReceivedMessageFunc(ProcessMessageEventArgs args)
        {
            const string ResultedLinkKey = "ResultedLink";
            const string SavedToHistoryKey = "SavedToHistory";
            var now = DateTime.UtcNow;
            var applicationProperties = args.Message.ApplicationProperties;

            _logger.LogInformation($"The message ({args.Identifier}) with content ({args.Message}) has been received.");

            var message = JsonSerializer.Deserialize<LongRunningOperationRequestModel>(args.Message.Body.ToStream())!;

            await using var scope = _serviceScopeFactory.CreateAsyncScope();

            // NOTE: do not inject provider in ctor to limit the scope where instance of dbcontext and repository are created.
            var longRunningOperationRepository = scope.ServiceProvider.GetRequiredService<ILongRunningOperationRepository>();
            var historyRepository = scope.ServiceProvider.GetRequiredService<IHistoryRepository>();
            IEnumerable<dynamic> longRunningOperationResult;

            try
            {
                // 1. Operation itself
                longRunningOperationResult = await longRunningOperationRepository.GetLongRunningOperationResultAsync(query: message.Query, longRunning: true, _mainCancellationToken);
            }
            catch (Exception ex)
            {
                // no need to save anything at this point
                _logger.LogError(ex, "Long running operation has been failed.");
                throw;
            }

            if (!TryGetValueFromAplicationProperties( // TODO: can metadata expire?
                    applicationProperties, 
                    key: ResultedLinkKey, 
                    valueFactory: (str) => new Uri(str)!, 
                    out var resultedLink))
            {
                try
                {
                    // 2. save to blob
                    resultedLink = await GetOrAddFileToBlobAsync(_blobContainer, message, longRunningOperationResult, _backgroundWorkerSettings, _mainCancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Saving to blob has been failed");
                    // technically longRunningOperationResult can be saved,
                    // but the document can be too big for saving in metadata
                    // so just throw and try again from scratch
                    throw;
                }
            }

            if (!TryGetValueFromAplicationProperties( // TODO: can metadata expire?
                applicationProperties,
                key: SavedToHistoryKey,
                valueFactory: (str) => DateTime.Parse(str)!,
                out var savedToHistoryAt))
            {
                try
                {
                    // 3. save history
                    await historyRepository.CreateHistoryRecordAsync(
                        histories: [
                            new HistoryModel(
                            id: Guid.NewGuid(),
                            message.Query,
                            now,
                            resultedLink)
                        ],
                        cancellationToken: _mainCancellationToken);
                }
                catch (Exception ex)
                {
                    var modifiedProperties = new Dictionary<string, object>(applicationProperties);
                    modifiedProperties.AddOrUpdate(ResultedLinkKey, resultedLink!);

                    _logger.LogError(ex, "Saving history has been failed.");
                    return (modifiedProperties, false);
                    // Log and ignore it for now, let signal r try to propagate the result
                }
            }

            // 4. notify signalr
            try
            {
                // TODO: use particular client isntead All
                await _notificationHub.Clients.All.SendAsync(
                    method: "OnOperationComplited",
                    //index, query, createdAt, resulteUrl
                    arg1: Guid.NewGuid(), // TODO: review logic
                    arg2: message.Query,
                    arg3: now,
                    arg4: resultedLink,
                    cancellationToken: _mainCancellationToken);
            }
            catch (Exception ex)
            {
                var modifiedProperties = new Dictionary<string, object>(args.Message.ApplicationProperties);
                modifiedProperties.AddOrUpdate(SavedToHistoryKey, now);

                _logger.LogError(ex, "SignalR sending has been failed.");
                return (modifiedProperties, false);
            }

            _logger.LogInformation($"The message ({args.Identifier}) with content ({args.Message}) has been processed.");
            return (null, true);

            static async Task<Uri?> GetOrAddFileToBlobAsync(
                BlobContainerClient blobContainer,
                LongRunningOperationRequestModel message,
                IEnumerable<dynamic> longRunningOperationResult,
                BackgroundWorkerSettings backgroundWorkerSettings,
                CancellationToken cancellationToken)
            {
                // TODO: choose much better key than query, consider generating some hash or guid
                var blob = blobContainer.GetBlobClient(message.NormalizedQuery.ToString());
                if (await blob.ExistsAsync(cancellationToken))
                {
                    var properties = await blob.GetPropertiesAsync(
                        // TODO: enable filtering
                        // conditions: new BlobRequestConditions(), 
                        cancellationToken: cancellationToken);
                    if (!properties.Value.Metadata.TryGetValue(backgroundWorkerSettings.BlobCacheMetadataKey, out var expirationDate))
                    {
                        // we're here only if saving to blob was without filled metadata
                        throw new InvalidOperationException("TODO: must not happen. Add better handling.");
                    }
                    if (DateTime.Parse(expirationDate, CultureInfo.InvariantCulture) > DateTime.UtcNow)
                    {
                        return blob.Uri;
                    }
                }

                using var writeToMemoryStream = new MemoryStream();
                await JsonSerializer.SerializeAsync<dynamic>(
                    utf8Json: writeToMemoryStream,
                    longRunningOperationResult,
                    cancellationToken: cancellationToken);
                writeToMemoryStream.Position = 0; // put cursor to the start of the stream

                var blobResult = await blob.UploadAsync(
                    content: writeToMemoryStream,
                    options: new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders
                        {
                            ContentType = "application/json",
                            // Investigate exact behavior
                            // CacheControl = $"max-age={TimeSpan.FromDays(_backgroundWorkerSettings.BlobCacheValidDays).TotalSeconds}"
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            {
                                backgroundWorkerSettings.BlobCacheMetadataKey,
                                DateTime.UtcNow.AddDays(backgroundWorkerSettings.BlobCacheValidDays).ToString(CultureInfo.InvariantCulture)
                            }
                        }
                    },
                    cancellationToken);

                return blob.Uri;
            }

            static bool TryGetValueFromAplicationProperties<TValue>(
                IReadOnlyDictionary<string, object> properties, 
                string key, 
                Func<string, TValue> valueFactory,
                out TValue? value)
            {
                value = default;

                if (properties.TryGetValue(key, out var resultedLinkObj) &&
                    resultedLinkObj != null)
                {
                    value = (valueFactory ?? throw new ArgumentNullException(nameof(valueFactory)))(resultedLinkObj.ToString()!);
                    return true;
                }
                return false;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Consuming is starting..");

            await _serviceBusConsumer.StartListenAsync();

            _logger.LogInformation("Consuming has been started.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Consuming is stopping..");

            await _serviceBusConsumer.DisposeAsync();
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            _logger.LogInformation("Consuming has been stopped.");
        }
    }
}