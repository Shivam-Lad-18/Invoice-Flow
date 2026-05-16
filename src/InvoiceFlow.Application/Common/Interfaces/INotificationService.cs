using InvoiceFlow.Application.Common.Models;

namespace InvoiceFlow.Application.Common.Interfaces;

/// <summary>
/// Abstracts real-time push notifications via Azure SignalR.
/// Called by Azure Functions after processing events.
/// </summary>
public interface INotificationService
{
    Task SendToUserAsync(
        Guid userId,
        NotificationMessage message,
        CancellationToken cancellationToken = default);

    Task SendToUsersAsync(
        IEnumerable<Guid> userIds,
        NotificationMessage message,
        CancellationToken cancellationToken = default);
}
