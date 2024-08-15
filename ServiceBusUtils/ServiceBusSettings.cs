using Microsoft.Extensions.Configuration;

namespace ServiceBusUtils
{
    public interface IServiceBusSettings
    {
        /// <summary>
        /// TODO: use passwordless approach instead.
        /// See for details https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues?tabs=connection-string#authenticate-the-app-to-azure
        /// </summary>
        string ServiceBusConnectionString { get; }
        string QueueName { get; }
    }

    public class AzureServiceBusSettings(IConfiguration configuration) : IServiceBusSettings
    {
        public string ServiceBusConnectionString => configuration.GetValue<string>(nameof(ServiceBusConnectionString)) ?? throw new ArgumentNullException(nameof(ServiceBusConnectionString));

        public string QueueName => configuration.GetValue<string>(nameof(QueueName)) ?? throw new ArgumentNullException(nameof(QueueName));
    }
}
