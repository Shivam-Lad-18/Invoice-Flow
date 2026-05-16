using InvoiceFlow.Domain.Entities;

namespace InvoiceFlow.Domain.Services;

/// <summary>
/// Creates <see cref="InvoiceLineItem"/> instances from AI extraction output.
/// Line items are immutable after creation — no mutation methods are provided.
/// </summary>
public sealed class InvoiceLineItemFactory
{
    /// <summary>Creates a single line item belonging to an extraction result.</summary>
    public InvoiceLineItem Create(
        Guid extractionResultId,
        string? description,
        decimal? quantity,
        decimal? unitPrice,
        decimal? amount,
        decimal confidence) => new()
        {
            ExtractionResultId = extractionResultId,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Amount = amount,
            Confidence = confidence
        };

    /// <summary>
    /// Convenience overload — creates a list of line items from raw extracted data.
    /// </summary>
    public IReadOnlyList<InvoiceLineItem> CreateMany(
        Guid extractionResultId,
        IEnumerable<(string? Description, decimal? Quantity, decimal? UnitPrice, decimal? Amount, decimal Confidence)> items)
        => items.Select(i => Create(extractionResultId, i.Description, i.Quantity, i.UnitPrice, i.Amount, i.Confidence))
                .ToList();
}
