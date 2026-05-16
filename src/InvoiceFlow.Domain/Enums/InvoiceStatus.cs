namespace InvoiceFlow.Domain.Enums;

/// <summary>
/// Represents the full lifecycle state of an invoice through the system.
/// Transitions are validated in Invoice.TransitionStatus().
/// </summary>
public enum InvoiceStatus
{
    /// <summary>File received and stored in Blob Storage. Extraction not yet started.</summary>
    Uploaded = 0,

    /// <summary>AI extraction job is in progress via Azure Function.</summary>
    Extracting = 1,

    /// <summary>AI extraction completed. May have low-confidence fields flagged for manual review.</summary>
    Extracted = 2,

    /// <summary>Extraction validated. Approval workflow created and waiting for first approver action.</summary>
    PendingApproval = 3,

    /// <summary>At least one approver has taken action. Workflow is progressing through steps.</summary>
    InApproval = 4,

    /// <summary>All required approval steps are approved. Invoice is fully processed.</summary>
    Approved = 5,

    /// <summary>At least one approver rejected. Or duplicate detected. Invoice is closed.</summary>
    Rejected = 6
}
