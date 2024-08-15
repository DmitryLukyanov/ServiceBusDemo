using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace ServiceBusUtils
{
    public class ServiceBusPublisher : IAsyncDisposable
    {
        private readonly CancellationToken _cancellationToken;
        private readonly ServiceBusClient _client;
        private readonly Lazy<ServiceBusSender> _senderLazy;
        private readonly IServiceBusSettings _settings;
        private readonly InterlockedInt32<RunningState> _state; // just in case
        private readonly string _queueName;

        public ServiceBusPublisher(IServiceBusSettings settings, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _queueName = settings.QueueName;
            _client = ServiceBusFactory.Create(settings);
            _senderLazy = new Lazy<ServiceBusSender>(() =>
            {
                var serviceBusSenderOptions = new ServiceBusSenderOptions();
                return _client.CreateSender(_queueName, serviceBusSenderOptions);
            }, 
            isThreadSafe: true);
            _state = new InterlockedInt32<RunningState>(RunningState.NotStarted);
        }

        public async ValueTask DisposeAsync()
        {
            if (_state.TryChange(RunningState.Disposed))
            {
                await _senderLazy.Value.DisposeAsync(); // close happens inside
                await _client.DisposeAsync();
            }
        }

        public async Task SendAsync<T>(T payload, string correlationId, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationToken);
            var effectiveCancellationToken = source.Token;

            string messagePayload = JsonSerializer.Serialize(payload);
            var messageId = Guid.NewGuid();
            var message = new ServiceBusMessage(messagePayload)
            {
                MessageId = messageId.ToString(),
            };
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                message.CorrelationId = correlationId;
            }
            //if (!message.ApplicationProperties.TryAdd())
            //{
            //    throw new InvalidOperationException("TODO: must not happen");
            //}
            await _senderLazy
                .Value // initialize sender
                .SendMessageAsync(message, effectiveCancellationToken);
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
