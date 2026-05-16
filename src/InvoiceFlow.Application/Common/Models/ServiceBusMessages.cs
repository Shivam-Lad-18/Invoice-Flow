namespace InvoiceFlow.Application.Common.Models;

/// <summary>Standardised service bus message payload for the invoice-extraction queue.</summary>
public sealed class InvoiceExtractionMessage
{
    public required Guid InvoiceId { get; init; }
    public required string BlobPath { get; init; }
    public required string CorrelationId { get; init; }
}

/// <summary>Standardised service bus message payload for the approval-notifications queue.</summary>
public sealed class ApprovalNotificationMessage
{
    public required string Type { get; init; }
    public required Guid InvoiceId { get; init; }
    public required IReadOnlyList<Guid> RecipientUserIds { get; init; }
    public Dictionary<string, object> Payload { get; init; } = [];
}
