namespace InvoiceFlow.Application.Common.Interfaces;

/// <summary>
/// Abstracts Azure Service Bus publishing.
/// The queue name determines routing (invoice-extraction or approval-notifications).
/// </summary>
public interface IServiceBusPublisher
{
    Task PublishAsync<T>(
        string queueName,
        T message,
        CancellationToken cancellationToken = default) where T : class;
}
