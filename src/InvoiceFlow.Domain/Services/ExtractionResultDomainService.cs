using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Domain.Events;

namespace InvoiceFlow.Domain.Services;

/// <summary>
/// Handles creation and manual correction of <see cref="ExtractionResult"/>.
/// Raises <see cref="ExtractionCompletedEvent"/> on creation to trigger workflow and notifications.
/// </summary>
public sealed class ExtractionResultDomainService
{
    /// <summary>
    /// Creates an extraction result from AI Document Intelligence output
    /// and raises <see cref="ExtractionCompletedEvent"/>.
    /// </summary>
    public ExtractionResult Create(
        Guid invoiceId,
        string? vendorName,
        string? invoiceNumber,
        DateTime? invoiceDate,
        DateTime? dueDate,
        decimal? totalAmount,
        decimal? subTotal,
        decimal? taxAmount,
        string? currency,
        string confidenceScoresJson,
        bool hasLowConfidenceFields)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(confidenceScoresJson);

        var result = new ExtractionResult
        {
            InvoiceId = invoiceId,
            VendorName = vendorName,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            DueDate = dueDate,
            TotalAmount = totalAmount,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            Currency = currency,
            ConfidenceScores = confidenceScoresJson,
            HasLowConfidenceFields = hasLowConfidenceFields,
            ExtractedAt = DateTime.UtcNow
        };

        result.RaiseDomainEvent(new ExtractionCompletedEvent(
            invoiceId, result.Id, hasLowConfidenceFields, totalAmount));

        return result;
    }

    /// <summary>
    /// Applies manual corrections to AI-extracted fields after a user reviews low-confidence data.
    /// Records the correcting user and timestamp.
    /// </summary>
    public void ApplyManualCorrection(
        ExtractionResult result,
        string? vendorName,
        string? invoiceNumber,
        DateTime? invoiceDate,
        DateTime? dueDate,
        decimal? totalAmount,
        decimal? subTotal,
        decimal? taxAmount,
        string? currency,
        Guid correctedByUserId)
    {
        ArgumentNullException.ThrowIfNull(result);

        result.VendorName = vendorName;
        result.InvoiceNumber = invoiceNumber;
        result.InvoiceDate = invoiceDate;
        result.DueDate = dueDate;
        result.TotalAmount = totalAmount;
        result.SubTotal = subTotal;
        result.TaxAmount = taxAmount;
        result.Currency = currency;
        result.IsManuallyCorrected = true;
        result.CorrectedAt = DateTime.UtcNow;
        result.CorrectedByUserId = correctedByUserId;
        result.MarkUpdated();
    }
}
