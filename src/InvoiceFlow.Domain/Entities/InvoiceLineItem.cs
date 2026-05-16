using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Entities;

/// <summary>
/// A single AI-extracted invoice line item — state only.
/// Created via <see cref="Services.InvoiceLineItemFactory"/>.
/// </summary>
public sealed class InvoiceLineItem : BaseEntity
{
    public Guid ExtractionResultId { get; internal set; }
    public string? Description { get; internal set; }
    public decimal? Quantity { get; internal set; }
    public decimal? UnitPrice { get; internal set; }
    public decimal? Amount { get; internal set; }

    /// <summary>AI confidence score for this line item (0.0 – 1.0).</summary>
    public decimal Confidence { get; internal set; }

    // Navigation — populated by EF Core
    public ExtractionResult? ExtractionResult { get; private set; }

    internal InvoiceLineItem() { } // EF Core + InvoiceLineItemFactory
}
