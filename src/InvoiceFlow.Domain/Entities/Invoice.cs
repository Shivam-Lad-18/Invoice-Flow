using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Enums;

namespace InvoiceFlow.Domain.Entities;

/// <summary>
/// Core aggregate root — invoice state and identity only.
/// All creation and mutation is handled by <see cref="Services.InvoiceDomainService"/>.
/// </summary>
public sealed class Invoice : BaseEntity
{
    public Guid VendorId { get; internal set; }

    /// <summary>Blob Storage path: invoices/{year}/{month}/{guid}{ext}</summary>
    public string BlobPath { get; internal set; } = string.Empty;

    public string OriginalFileName { get; internal set; } = string.Empty;
    public long FileSizeBytes { get; internal set; }
    public InvoiceStatus Status { get; internal set; } = InvoiceStatus.Uploaded;
    public Guid UploadedByUserId { get; internal set; }
    public DateTime UploadedAt { get; internal set; }

    /// <summary>
    /// SHA-256 hash of (VendorId + InvoiceNumber + TotalAmount).
    /// Computed after extraction. Used for O(1) duplicate detection.
    /// </summary>
    public string? DuplicateCheckHash { get; internal set; }

    // Navigation — populated by EF Core
    public Vendor? Vendor { get; private set; }
    public ExtractionResult? ExtractionResult { get; private set; }
    public ApprovalWorkflow? ApprovalWorkflow { get; private set; }
    public ICollection<AuditLog> AuditLogs { get; private set; } = [];

    internal Invoice() { } // EF Core + InvoiceDomainService
}
