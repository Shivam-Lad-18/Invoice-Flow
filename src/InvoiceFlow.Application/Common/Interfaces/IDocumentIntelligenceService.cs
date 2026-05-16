using InvoiceFlow.Application.Common.Models;

namespace InvoiceFlow.Application.Common.Interfaces;

/// <summary>
/// Abstracts Azure AI Document Intelligence calls.
/// Uses the prebuilt invoice model — no training required.
/// Confidence scores below 0.70 trigger manual review flags.
/// </summary>
public interface IDocumentIntelligenceService
{
    /// <summary>
    /// Downloads the blob at the given path and runs the prebuilt invoice model.
    /// Returns structured extraction data with per-field confidence scores.
    /// </summary>
    Task<InvoiceExtractionResult> ExtractInvoiceAsync(
        string blobPath,
        CancellationToken cancellationToken = default);
}
