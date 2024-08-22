using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Models;
using BackgroundWorker.Repositories;
using BackgroundWorker.SignalR;
using BackgroundWorker.Utils;
using ServiceBusUtils;
using System.Globalization;
using System.Text.Json;
using System.Diagnostics;

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
        private readonly SignalRUtils _notificationHub;
        private readonly ServiceBusConsumer _serviceBusConsumer;
        private readonly IServiceBusSettings _serviceBusSettings;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ServiceBusHostedService(
            IServiceBusSettings serviceBusSettings,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ServiceBusHostedService> logger,
            BackgroundWorkerSettings backgroundWorkerSettings,
            BlobServiceClient blobServiceClient,
            SignalRUtils notificationHub)
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
            // such messages will appear in dead letter queue, handle it there
            return Task.CompletedTask;
        }

        private async Task<(Dictionary<string, object>? ModifiedProperties, bool Success)> ReceivedMessageFunc(ProcessMessageEventArgs args)
        {
            const string ResultedLinkKey = "ResultedLink";
            const string HistoryIdKey = "HistoryId";
            const string HistoryIdCompletedKey = "HistoryIdCompleted";

            var applicationProperties = args.Message.ApplicationProperties;
            var modifiedProperties = new Dictionary<string, object>(applicationProperties);

            LogMessage($"Message has been received.");

            var message = JsonSerializer.Deserialize<LongRunningOperationRequestModel>(args.Message.Body.ToStream())!;

            await using var scope = _serviceScopeFactory.CreateAsyncScope();

            // NOTE: do not inject provider in ctor to limit the scope where instance of dbcontext and repository are created.
            var longRunningOperationRepository = scope.ServiceProvider.GetRequiredService<ILongRunningOperationRepository>();
            var historyRepository = scope.ServiceProvider.GetRequiredService<IHistoryRepository>();

            // 1. Start processing
            var createdAt = DateTime.UtcNow;
            if (!TryGetValueFromAplicationProperties( // TODO: can metadata expire?
                applicationProperties,
                key: HistoryIdKey,
                valueFactory: (str) => Guid.Parse(str)!,
                out var historyId))
            {
                historyId = message.Id;
                try
                {
                    // initialize processing
                    await historyRepository.CreateHistoryRecordsAsync([
                        new HistoryModel(
                            historyId,
                            message.Query,
                            createdAt,
                            message.UserName)
                    ]);
                    modifiedProperties.AddOrUpdate(HistoryIdKey, historyId);
                }
                catch (Exception ex)
                {
                    LogMessage($"History initialization has been failed", ex);
                    return (modifiedProperties, false);
                }
            }

            var started = DateTime.UtcNow;
            // 2. notify signalr
            try
            {
                await _notificationHub.SendAsync(
                    javascriptMethodName: "OnOperationNotified",
                    userName: message.UserName,
                    arg1: historyId,
                    arg2: message.Query,
                    arg3: started,
                    arg4: null,
                    arg5: null,
                    arg6: null,
                    cancellationToken: _mainCancellationToken);
            }
            catch (Exception ex)
            {
                LogMessage("SignalR sending has been failed", ex);
                // ignore it since it's minor intermediate notification
            }

            IEnumerable<dynamic> longRunningOperationResult;
            var stopWatch = Stopwatch.StartNew();

            // 3. Operation itself
            try
            {
                longRunningOperationResult = await longRunningOperationRepository.GetLongRunningOperationResultAsync(query: message.Query, longRunning: true, _mainCancellationToken);
                // technically longRunningOperationResult can be saved,
                // but the document can be too big for saving in metadata
                // so just throw and try again from scratch
            }
            catch (Exception ex)
            {
                // no need to save anything at this point
                LogMessage("Long running operation has been failed", ex);
                return (modifiedProperties, false);
            }
            finally
            {
                stopWatch.Stop();
            }

            // 4. save to blob
            if (!TryGetValueFromAplicationProperties( // TODO: can metadata expire?
                    applicationProperties,
                    key: ResultedLinkKey,
                    valueFactory: (str) => new Uri(str)!,
                    out var resultedLink))
            {
                try
                {
                    resultedLink = await GetOrAddFileToBlobAsync(_blobContainer, message, longRunningOperationResult, _backgroundWorkerSettings, _mainCancellationToken);
                    modifiedProperties.AddOrUpdate(ResultedLinkKey, resultedLink!);
                }
                catch (Exception ex)
                {
                    LogMessage("Saving to blob has been failed", ex);
                    return (modifiedProperties, false);
                }
            }

            // 5. save history
            var completed = DateTime.UtcNow;
            if (!TryGetValueFromAplicationProperties( // TODO: can metadata expire?
                applicationProperties,
                key: HistoryIdCompletedKey,
                valueFactory: (str) => DateTime.Parse(str)!,
                out var savedToHistoryAt))
            {
                try
                {
                    await historyRepository.UpdateHistoryRecordAsync(
                        historyId,
                        resultedLink!,
                        stopWatch.Elapsed,
                        completed,
                        cancellationToken: _mainCancellationToken);
                    modifiedProperties.AddOrUpdate(HistoryIdCompletedKey, completed);
                }
                catch (Exception ex)
                {
                    LogMessage("Saving history has been failed", ex);
                    return (modifiedProperties, false);
                }
            }

            // 6. notify signalr
            try
            {
                await _notificationHub.SendAsync(
                    javascriptMethodName: "OnOperationNotified",
                    userName: message.UserName,
                    arg1: historyId,
                    arg2: message.Query,
                    arg3: started,
                    arg4: resultedLink,
                    arg5: stopWatch.Elapsed,
                    arg6: completed,
                    cancellationToken: _mainCancellationToken);
            }
            catch (Exception ex)
            {
                LogMessage("SignalR sending has been failed", ex);
                return (modifiedProperties, false); // TODO: maybe it makes sense to ignore this notification too
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
                // TODO: add timeout for cache / TTL
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
                            // TODO: Investigate exact behavior
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

            void LogMessage(string message, Exception? ex = null)
            {
                var logMessagePrefix = $"{args.Identifier}: Delivery count: {args.Message.DeliveryCount}";
                if (ex == null)
                {
                    _logger.LogInformation($"{logMessagePrefix}.{message}.");
                }
                else
                {
                    _logger.LogError(ex, $"{logMessagePrefix}.{message}.");
                }
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