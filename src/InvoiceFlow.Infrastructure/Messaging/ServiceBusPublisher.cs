using Azure.Messaging.ServiceBus;
using InvoiceFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InvoiceFlow.Infrastructure.Messaging;

/// <summary>
/// Publishes messages to Azure Service Bus queues.
/// If the connection string is empty (local dev without Service Bus),
/// the publish is skipped with a warning rather than crashing.
/// </summary>
public sealed class ServiceBusPublisher(
    IConfiguration configuration,
    ILogger<ServiceBusPublisher> logger) : IServiceBusPublisher, IAsyncDisposable
{
    private readonly string? _connectionString = configuration["Azure:ServiceBus:ConnectionString"];
    private ServiceBusClient? _client;

    public async Task PublishAsync<T>(
        string queueName,
        T message,
        CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            logger.LogWarning(
                "Azure:ServiceBus:ConnectionString is not configured. " +
                "Skipping publish to queue '{Queue}'. Configure it to enable async extraction.",
                queueName);
            return;
        }

        _client ??= new ServiceBusClient(_connectionString);
        await using var sender = _client.CreateSender(queueName);

        var json = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(json) { ContentType = "application/json" };

        await sender.SendMessageAsync(sbMessage, cancellationToken);

        logger.LogInformation("Published {MessageType} to queue '{Queue}'.",
            typeof(T).Name, queueName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
            await _client.DisposeAsync();
    }
}
