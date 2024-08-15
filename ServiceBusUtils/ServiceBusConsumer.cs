using Azure.Messaging.ServiceBus;

namespace ServiceBusUtils
{
    public sealed class ServiceBusConsumer : IAsyncDisposable
    {
        private readonly CancellationToken _cancellationToken;
        private readonly ServiceBusClient _client;
        private readonly int _maxConcurrentCalls;
        private readonly Func<ProcessErrorEventArgs, Task> _receivedErrorFunc;
        private readonly Func<ProcessMessageEventArgs, Task<(Dictionary<string, object>? ModifiedProperties, bool Success)>> _receivedMessageFunc;
        private readonly Lazy<ServiceBusProcessor> _serviceBusProcessorLazy;
        private readonly string _queueName;
        private InterlockedInt32<RunningState> _state; // just in case

        public ServiceBusConsumer(
            IServiceBusSettings settings,
            Func<ProcessMessageEventArgs, Task<(Dictionary<string, object>? ModifiedProperties, bool Success)>> receivedMessageFunc,
            Func<ProcessErrorEventArgs, Task> receivedErrorFunc,
            int maxConcurrentCalls = 1,
            SubQueue subQueue = SubQueue.None,
            bool autoCompleteMessages = false,
            CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            _maxConcurrentCalls = maxConcurrentCalls;
            _receivedErrorFunc = receivedErrorFunc ?? throw new ArgumentNullException(nameof(receivedErrorFunc));
            _receivedMessageFunc = receivedMessageFunc ?? throw new ArgumentNullException(nameof(receivedMessageFunc));
            _state = new InterlockedInt32<RunningState>(RunningState.NotStarted);

            _client = ServiceBusFactory.Create(settings);
            _queueName = settings.QueueName;
            _serviceBusProcessorLazy = new Lazy<ServiceBusProcessor>(
                () =>
                {
                    var _serviceBusProcessorOptions = new ServiceBusProcessorOptions
                    {
                        MaxConcurrentCalls = _maxConcurrentCalls,
                        AutoCompleteMessages = autoCompleteMessages,
                        SubQueue = subQueue,
                    };
                    return _client.CreateProcessor(_queueName, _serviceBusProcessorOptions);
                }, 
                isThreadSafe: true);
        }

        public async Task StartListenAsync()
        {
            ThrowIfDisposed();
            if (_state.TryChange(RunningState.NotStarted, RunningState.InProgress))
            {
                var serviceBusProcessor = _serviceBusProcessorLazy.Value; // initialize processor
                serviceBusProcessor.ProcessMessageAsync += ReceivedMessageWithCompletionAsync;
                serviceBusProcessor.ProcessErrorAsync += _receivedErrorFunc;
                await serviceBusProcessor.StartProcessingAsync(_cancellationToken); // supposed to be long running
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_state.TryChange(RunningState.Disposed))
            {
                var serviceBusProcessor = _serviceBusProcessorLazy.Value;
                serviceBusProcessor.ProcessMessageAsync -= ReceivedMessageWithCompletionAsync;
                serviceBusProcessor.ProcessErrorAsync -= _receivedErrorFunc;
                try { await serviceBusProcessor.StopProcessingAsync(_cancellationToken); } catch { /* not expected to happen, but even if so, ignore it for now */ }
                try { await serviceBusProcessor.DisposeAsync(); } catch { /* not expected to happen, but even if so, ignore it for now*/ }
                await _client.DisposeAsync();
            }
        }

        private async Task ReceivedMessageWithCompletionAsync(ProcessMessageEventArgs processMessageEventArgs)
        {
            using var effectiveCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                processMessageEventArgs.CancellationToken, // dependent on StopProcessingAsync
                _cancellationToken);
            var effectiveCancellationToken = effectiveCancellationTokenSource.Token;
            (Dictionary<string, object>? ModifiedProperties, bool Success) result;
            try
            {
                effectiveCancellationToken.ThrowIfCancellationRequested();
                result = await _receivedMessageFunc(processMessageEventArgs); // external call
            }
            catch
            {
                result = (null, false);
            }

            if (!result.Success)
            {
                // will increase delivery count
                await processMessageEventArgs.AbandonMessageAsync(
                    processMessageEventArgs.Message,
                    propertiesToModify: result.ModifiedProperties,
                    effectiveCancellationToken);
            }
            else
            {
                await processMessageEventArgs.CompleteMessageAsync(processMessageEventArgs.Message, effectiveCancellationToken);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_state.Value == (int)RunningState.Disposed)
            {
                throw new ObjectDisposedException(nameof(ServiceBusPublisher));
            }
        }
    }
}
