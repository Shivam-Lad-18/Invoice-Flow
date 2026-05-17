using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Storage.Blobs;
using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Domain.Enums;
using InvoiceFlow.Domain.Services;
using InvoiceFlow.Functions.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InvoiceFlow.Functions.Functions;

/// <summary>
/// Service Bus triggered function that extracts invoice data using Azure Document Intelligence.
/// Triggered by messages published to the "invoice-extraction" queue by the API after upload.
///
/// Flow:
///   API uploads blob → publishes to Service Bus → this function triggers →
///   calls Document Intelligence → saves ExtractionResult → transitions Invoice to Extracted
/// </summary>
public sealed class InvoiceExtractionFunction(
    FunctionsDbContext db,
    DocumentIntelligenceClient diClient,
    BlobContainerClient blobContainer,
    ILogger<InvoiceExtractionFunction> logger)
{
    private const float LowConfidenceThreshold = 0.70f;
    private const string PrebuiltInvoiceModel = "prebuilt-invoice";

    [Function(nameof(InvoiceExtractionFunction))]
    public async Task Run(
        [ServiceBusTrigger("invoice-extraction", Connection = "ServiceBusConnection")] string messageBody,
        FunctionContext context,
        CancellationToken ct)
    {
        var message = JsonSerializer.Deserialize<ExtractionMessage>(messageBody);
        if (message is null)
        {
            logger.LogError("Failed to deserialize extraction message: {Body}", messageBody);
            return;
        }

        logger.LogInformation(
            "Processing invoice extraction. InvoiceId={InvoiceId}, BlobPath={BlobPath}, CorrelationId={CorrelationId}",
            message.InvoiceId, message.BlobPath, message.CorrelationId);

        var invoice = await db.Invoices.FindAsync([message.InvoiceId], ct);
        if (invoice is null)
        {
            logger.LogWarning("Invoice {InvoiceId} not found. Skipping.", message.InvoiceId);
            return;
        }

        if (invoice.Status != InvoiceStatus.Extracting)
        {
            logger.LogWarning("Invoice {InvoiceId} is in status {Status}, expected Extracting. Skipping.",
                message.InvoiceId, invoice.Status);
            return;
        }

        try
        {
            // 1. Download invoice blob
            var blobData = await DownloadBlobAsync(message.BlobPath, ct);

            // 2. Call Document Intelligence
            var operation = await diClient.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                PrebuiltInvoiceModel,
                BinaryData.FromBytes(blobData),
                cancellationToken: ct);

            var result = operation.Value;
            var doc = result.Documents?.FirstOrDefault();

            if (doc is null)
            {
                logger.LogWarning("Document Intelligence returned no documents for invoice {InvoiceId}.", message.InvoiceId);
                await FailInvoiceAsync(invoice, "Document Intelligence returned no documents.", ct);
                return;
            }

            // 3. Parse extracted fields (pass raw response for line item JSON parsing)
            var extracted = ParseInvoiceDocument(doc, operation.GetRawResponse().Content);

            // 4. Build confidence scores JSON
            var confidenceJson = JsonSerializer.Serialize(extracted.Confidences);
            var hasLowConfidence = extracted.Confidences.Values.Any(c => c < LowConfidenceThreshold);

            // 5. Create ExtractionResult via domain service
            var extractionService = new ExtractionResultDomainService();
            var extractionResult = extractionService.Create(
                invoiceId: invoice.Id,
                vendorName: extracted.VendorName,
                invoiceNumber: extracted.InvoiceNumber,
                invoiceDate: extracted.InvoiceDate,
                dueDate: extracted.DueDate,
                totalAmount: extracted.TotalAmount,
                subTotal: extracted.SubTotal,
                taxAmount: extracted.TaxAmount,
                currency: extracted.Currency,
                confidenceScoresJson: confidenceJson,
                hasLowConfidenceFields: hasLowConfidence);

            // 6. Create line items
            var lineItemFactory = new InvoiceLineItemFactory();
            var lineItems = lineItemFactory.CreateMany(extractionResult.Id, extracted.LineItems);

            // 7. Set duplicate check hash (VendorName + InvoiceNumber + TotalAmount)
            var invoiceService = new InvoiceDomainService();
            if (!string.IsNullOrEmpty(extracted.InvoiceNumber) && extracted.TotalAmount.HasValue)
            {
                var raw = $"{extracted.VendorName}|{extracted.InvoiceNumber}|{extracted.TotalAmount}";
                var hash = Convert.ToHexString(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(raw)));
                invoiceService.SetDuplicateCheckHash(invoice, hash);
            }

            // 8. Transition Invoice: Extracting → Extracted
            invoiceService.TransitionStatus(invoice, InvoiceStatus.Extracted);

            // 9. Audit log
            var auditFactory = new AuditLogFactory();
            var audit = auditFactory.Create(
                action: "INVOICE_EXTRACTED",
                invoiceId: invoice.Id,
                newValue: $"{{\"hasLowConfidence\":{hasLowConfidence.ToString().ToLower()},\"correlationId\":\"{message.CorrelationId}\"}}",
                correlationId: message.CorrelationId);

            // 10. Persist
            db.ExtractionResults.Add(extractionResult);
            db.InvoiceLineItems.AddRange(lineItems);
            db.AuditLogs.Add(audit);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Extraction complete for invoice {InvoiceId}. HasLowConfidence={HasLowConfidence}, LineItems={Count}.",
                invoice.Id, hasLowConfidence, lineItems.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Extraction failed for invoice {InvoiceId}.", message.InvoiceId);
            await FailInvoiceAsync(invoice, ex.Message, ct);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<byte[]> DownloadBlobAsync(string blobPath, CancellationToken ct)
    {
        // blobPath is the blob name within the container, e.g. "invoices/2026/05/{guid}.png"
        // The container client is pre-configured with the correct container ("invoices").
        var blob = blobContainer.GetBlobClient(blobPath);

        using var ms = new MemoryStream();
        await blob.DownloadToAsync(ms, ct);
        return ms.ToArray();
    }

    private async Task FailInvoiceAsync(Invoice invoice, string reason, CancellationToken ct)
    {
        try
        {
            var invoiceService = new InvoiceDomainService();
            invoiceService.TransitionStatus(invoice, InvoiceStatus.Rejected);

            var auditFactory = new AuditLogFactory();
            var audit = auditFactory.Create(
                action: "INVOICE_EXTRACTION_FAILED",
                invoiceId: invoice.Id,
                newValue: $"{{\"reason\":\"{reason.Replace("\"", "\\\"").Replace("\n", " ")}\"}}");

            db.AuditLogs.Add(audit);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update invoice {InvoiceId} to Rejected status.", invoice.Id);
        }
    }

    private ExtractedInvoiceData ParseInvoiceDocument(AnalyzedDocument doc, BinaryData rawResponse)
    {
        var confidences = new Dictionary<string, float>();

        string? GetString(string key)
        {
            if (!doc.Fields.TryGetValue(key, out var field)) return null;
            confidences[key] = field.Confidence ?? 0f;
            return field.Content;
        }

        DateTime? GetDate(string key)
        {
            if (!doc.Fields.TryGetValue(key, out var field)) return null;
            confidences[key] = field.Confidence ?? 0f;
            // ValueDate is DateTimeOffset? in Azure.AI.DocumentIntelligence 1.0.0
            return field.ValueDate.HasValue ? field.ValueDate.Value.UtcDateTime : null;
        }

        decimal? GetAmount(string key)
        {
            if (!doc.Fields.TryGetValue(key, out var field)) return null;
            confidences[key] = field.Confidence ?? 0f;
            return field.ValueCurrency?.Amount is double amt ? (decimal)amt : null;
        }

        var lineItems = new List<(string? Description, decimal? Quantity, decimal? UnitPrice, decimal? Amount, decimal Confidence)>();

        // Parse line items from raw JSON response (avoids SDK property naming ambiguity).
        // The DI JSON uses camelCase: valueArray / valueObject / valueCurrency / valueNumber.
        try
        {
            using var jsonDoc = JsonDocument.Parse(rawResponse);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("analyzeResult", out var ar) &&
                ar.TryGetProperty("documents", out var docs) &&
                docs.GetArrayLength() > 0 &&
                docs[0].TryGetProperty("fields", out var fields) &&
                fields.TryGetProperty("Items", out var itemsEl) &&
                itemsEl.TryGetProperty("valueArray", out var valueArray))
            {
                foreach (var item in valueArray.EnumerateArray())
                {
                    if (!item.TryGetProperty("valueObject", out var obj)) continue;

                    var conf = item.TryGetProperty("confidence", out var cp)
                        ? (decimal)cp.GetDouble() : 0.5m;

                    string? desc = null;
                    if (obj.TryGetProperty("Description", out var df) &&
                        df.TryGetProperty("content", out var dc))
                        desc = dc.GetString();

                    decimal? qty = null;
                    if (obj.TryGetProperty("Quantity", out var qf) &&
                        qf.TryGetProperty("valueNumber", out var qv))
                        qty = (decimal)qv.GetDouble();

                    decimal? unitPrice = null;
                    if (obj.TryGetProperty("UnitPrice", out var uf) &&
                        uf.TryGetProperty("valueCurrency", out var uc) &&
                        uc.TryGetProperty("amount", out var ua))
                        unitPrice = (decimal)ua.GetDouble();

                    decimal? amt = null;
                    if (obj.TryGetProperty("Amount", out var af) &&
                        af.TryGetProperty("valueCurrency", out var ac) &&
                        ac.TryGetProperty("amount", out var aa))
                        amt = (decimal)aa.GetDouble();

                    lineItems.Add((desc, qty, unitPrice, amt, conf));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse line items from raw DI response.");
        }

        return new ExtractedInvoiceData(
            VendorName: GetString("VendorName"),
            InvoiceNumber: GetString("InvoiceId"),
            InvoiceDate: GetDate("InvoiceDate"),
            DueDate: GetDate("DueDate"),
            TotalAmount: GetAmount("InvoiceTotal"),
            SubTotal: GetAmount("SubTotal"),
            TaxAmount: GetAmount("TotalTax"),
            Currency: GetString("CurrencyCode"),
            Confidences: confidences,
            LineItems: lineItems);
    }
}

// ── Internal models ───────────────────────────────────────────────────────────

internal sealed record ExtractionMessage(
    Guid InvoiceId,
    string BlobPath,
    string CorrelationId);

internal sealed record ExtractedInvoiceData(
    string? VendorName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    decimal? TotalAmount,
    decimal? SubTotal,
    decimal? TaxAmount,
    string? Currency,
    Dictionary<string, float> Confidences,
    List<(string? Description, decimal? Quantity, decimal? UnitPrice, decimal? Amount, decimal Confidence)> LineItems);
