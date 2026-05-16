namespace InvoiceFlow.Application.Common.Models;

/// <summary>Payload for real-time SignalR push notifications.</summary>
public sealed class NotificationMessage
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public Guid? InvoiceId { get; init; }
    public Dictionary<string, object> Payload { get; init; } = [];

    public static NotificationMessage ExtractionComplete(Guid invoiceId, bool hasLowConfidence) => new()
    {
        Type = "ExtractionComplete",
        Title = "Invoice Extracted",
        Body = hasLowConfidence
            ? "Extraction complete. Some fields need manual review."
            : "Extraction complete. Invoice is ready for approval.",
        InvoiceId = invoiceId
    };

    public static NotificationMessage ApprovalRequired(Guid invoiceId, string invoiceRef) => new()
    {
        Type = "ApprovalRequired",
        Title = "Approval Required",
        Body = $"Invoice {invoiceRef} is awaiting your approval.",
        InvoiceId = invoiceId
    };

    public static NotificationMessage StepDecided(Guid invoiceId, string decision) => new()
    {
        Type = "StepDecided",
        Title = "Approval Decision",
        Body = $"An approval step was {decision.ToLowerInvariant()}.",
        InvoiceId = invoiceId
    };

    public static NotificationMessage Escalation(Guid invoiceId) => new()
    {
        Type = "Escalation",
        Title = "Approval Escalated",
        Body = "An overdue approval step has been escalated.",
        InvoiceId = invoiceId
    };
}
