using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Text.Json;


namespace Icp.Messaging.ServiceBus;

public sealed class ServiceBusClientWrapper : IServiceBusClient, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusClientWrapper(IConfiguration configuration)
    {
        var domain = configuration["ServiceBus:Domain"]
            ?? throw new InvalidOperationException("ServiceBus domain missing");

        var queueName = configuration["ServiceBus:QueueName"]
            ?? throw new InvalidOperationException("ServiceBus queue name missing");

        _client = new ServiceBusClient(domain, new DefaultAzureCredential());
        _sender = _client.CreateSender(queueName);
    }

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(message);

        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json"
        };

        await _sender.SendMessageAsync(sbMessage, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}

