namespace InvoiceFlow.Application.Common.Models;

/// <summary>
/// Structured data extracted from an invoice by Azure AI Document Intelligence.
/// Returned by IDocumentIntelligenceService and mapped to ExtractionResult entity.
/// </summary>
public sealed class InvoiceExtractionResult
{
    public string? VendorName { get; init; }
    public string? InvoiceNumber { get; init; }
    public DateTime? InvoiceDate { get; init; }
    public DateTime? DueDate { get; init; }
    public decimal? TotalAmount { get; init; }
    public decimal? SubTotal { get; init; }
    public decimal? TaxAmount { get; init; }
    public string? Currency { get; init; }

    /// <summary>Per-field confidence scores (0.0–1.0). Fields below 0.70 require manual correction.</summary>
    public Dictionary<string, double> ConfidenceScores { get; init; } = [];

    public bool HasLowConfidenceFields =>
        ConfidenceScores.Values.Any(score => score < 0.70);

    public IReadOnlyList<ExtractedLineItem> LineItems { get; init; } = [];
}

public sealed class ExtractedLineItem
{
    public string? Description { get; init; }
    public decimal? Quantity { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? Amount { get; init; }
    public double Confidence { get; init; }
}
