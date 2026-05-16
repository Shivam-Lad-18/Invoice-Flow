using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Events;

/// <summary>
/// Raised when an invoice is successfully uploaded and stored in Blob Storage.
/// Triggers the async AI extraction job via Service Bus.
/// </summary>
public sealed record InvoiceUploadedEvent(
    Guid InvoiceId,
    Guid VendorId,
    string BlobPath) : IDomainEvent;
