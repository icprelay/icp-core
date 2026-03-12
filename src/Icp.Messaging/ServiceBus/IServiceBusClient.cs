namespace Icp.Messaging.ServiceBus;

public interface IServiceBusClient
{
    Task SendAsync<T>(T message, CancellationToken cancellationToken = default);
}
