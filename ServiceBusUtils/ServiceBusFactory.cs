using Azure.Messaging.ServiceBus;

namespace ServiceBusUtils
{
    public sealed class ServiceBusFactory
    {
        private ServiceBusFactory() { }

        public static ServiceBusClient Create(IServiceBusSettings settings) => 
            new ServiceBusClient(
                settings.ServiceBusConnectionString, 
                new ServiceBusClientOptions()
                {
#if DEBUG
                    // TODO: probably not needed
                    ConnectionIdleTimeout = TimeSpan.FromMinutes(5),
#endif
                    TransportType = ServiceBusTransportType.AmqpWebSockets
                });
    }
}
