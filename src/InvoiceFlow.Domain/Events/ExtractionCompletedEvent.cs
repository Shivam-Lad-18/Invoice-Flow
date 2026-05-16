using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Events;

/// <summary>
/// Raised when the AI extraction Azure Function completes processing.
/// Triggers approval workflow creation and notifies relevant users.
/// </summary>
public sealed record ExtractionCompletedEvent(
    Guid InvoiceId,
    Guid ExtractionResultId,
    bool HasLowConfidenceFields,
    decimal? TotalAmount) : IDomainEvent;
