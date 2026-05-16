using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Domain.Enums;
using InvoiceFlow.Domain.Events;

namespace InvoiceFlow.Domain.Services;

/// <summary>
/// Handles all creation and state mutation for <see cref="Invoice"/>.
/// Owns the status state machine and enforces valid transitions.
/// Raises <see cref="InvoiceUploadedEvent"/> on creation.
/// </summary>
public sealed class InvoiceDomainService
{
    private static readonly Dictionary<InvoiceStatus, HashSet<InvoiceStatus>> ValidTransitions = new()
    {
        [InvoiceStatus.Uploaded]        = [InvoiceStatus.Extracting],
        [InvoiceStatus.Extracting]      = [InvoiceStatus.Extracted, InvoiceStatus.Rejected],
        [InvoiceStatus.Extracted]       = [InvoiceStatus.PendingApproval],
        [InvoiceStatus.PendingApproval] = [InvoiceStatus.InApproval],
        [InvoiceStatus.InApproval]      = [InvoiceStatus.Approved, InvoiceStatus.Rejected],
        [InvoiceStatus.Approved]        = [],
        [InvoiceStatus.Rejected]        = []
    };

    /// <summary>
    /// Creates a new invoice record and raises <see cref="InvoiceUploadedEvent"/>
    /// to trigger async AI extraction via Service Bus.
    /// </summary>
    public Invoice Create(
        Guid vendorId,
        string blobPath,
        string originalFileName,
        long fileSizeBytes,
        Guid uploadedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);

        var invoice = new Invoice
        {
            VendorId = vendorId,
            BlobPath = blobPath,
            OriginalFileName = originalFileName,
            FileSizeBytes = fileSizeBytes,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow
        };

        invoice.RaiseDomainEvent(new InvoiceUploadedEvent(invoice.Id, vendorId, blobPath));
        return invoice;
    }

    /// <summary>
    /// Transitions the invoice to a new status, enforcing the valid state machine.
    /// Throws <see cref="InvalidOperationException"/> for illegal transitions.
    /// </summary>
    public void TransitionStatus(Invoice invoice, InvoiceStatus newStatus)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        if (!ValidTransitions.TryGetValue(invoice.Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Invalid invoice status transition: {invoice.Status} → {newStatus}.");

        invoice.Status = newStatus;
        invoice.MarkUpdated();
    }

    /// <summary>
    /// Stores the SHA-256 duplicate-detection hash on the invoice.
    /// Called after AI extraction when InvoiceNumber + Amount are known.
    /// </summary>
    public void SetDuplicateCheckHash(Invoice invoice, string hash)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        invoice.DuplicateCheckHash = hash;
        invoice.MarkUpdated();
    }
}
