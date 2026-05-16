namespace InvoiceFlow.Domain.Common;

/// <summary>
/// Marker interface for all domain events raised by aggregate roots.
/// Domain events are dispatched by the Application layer after persistence.
/// </summary>
public interface IDomainEvent { }
