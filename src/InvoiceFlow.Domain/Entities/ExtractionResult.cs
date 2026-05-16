using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Entities;

/// <summary>
/// AI-extracted invoice data — state and identity only.
/// All creation and mutation is handled by <see cref="Services.ExtractionResultDomainService"/>.
/// </summary>
public sealed class ExtractionResult : BaseEntity
{
    public Guid InvoiceId { get; internal set; }

    public string? VendorName { get; internal set; }
    public string? InvoiceNumber { get; internal set; }
    public DateTime? InvoiceDate { get; internal set; }
    public DateTime? DueDate { get; internal set; }
    public decimal? TotalAmount { get; internal set; }
    public string? Currency { get; internal set; }
    public decimal? SubTotal { get; internal set; }
    public decimal? TaxAmount { get; internal set; }

    /// <summary>
    /// JSON object: { "VendorName": 0.95, "TotalAmount": 0.62, ... }
    /// Fields with confidence &lt; 0.70 are highlighted in the UI for manual correction.
    /// </summary>
    public string ConfidenceScores { get; internal set; } = "{}";

    /// <summary>True if any field has confidence below 0.70.</summary>
    public bool HasLowConfidenceFields { get; internal set; }

    public bool IsManuallyCorrected { get; internal set; }
    public DateTime ExtractedAt { get; internal set; }
    public DateTime? CorrectedAt { get; internal set; }
    public Guid? CorrectedByUserId { get; internal set; }

    // Navigation — populated by EF Core
    public Invoice? Invoice { get; private set; }
    public ICollection<InvoiceLineItem> LineItems { get; private set; } = [];

    internal ExtractionResult() { } // EF Core + ExtractionResultDomainService
}
