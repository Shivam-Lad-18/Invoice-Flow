namespace InvoiceFlow.Application.Common.Interfaces;

/// <summary>
/// Provides the current HTTP request's correlation ID.
/// Set by CorrelationIdMiddleware in the API. Written to all audit log entries.
/// </summary>
public interface ICorrelationIdService
{
    string CorrelationId { get; }
}
