using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Application.Common.Models;
using InvoiceFlow.Domain.Enums;
using InvoiceFlow.Domain.Services;
using MediatR;

namespace InvoiceFlow.Application.Features.Invoices.Commands;

public sealed record UploadInvoiceCommand(
    Guid VendorId,
    Stream FileStream,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    Guid UploadedByUserId) : IRequest<UploadInvoiceResponse>;

public sealed record UploadInvoiceResponse(
    Guid InvoiceId,
    string BlobPath,
    InvoiceStatus Status);

internal sealed class UploadInvoiceCommandHandler(
    IBlobStorageService blobStorage,
    IServiceBusPublisher serviceBus,
    IApplicationDbContext db,
    InvoiceDomainService invoiceService,
    AuditLogFactory auditLogFactory) : IRequestHandler<UploadInvoiceCommand, UploadInvoiceResponse>
{
    private const string ExtractionQueue = "invoice-extraction";

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff"
    ];

    public async Task<UploadInvoiceResponse> Handle(UploadInvoiceCommand request, CancellationToken ct)
    {
        if (!AllowedContentTypes.Contains(request.ContentType.ToLowerInvariant()))
            throw new InvalidOperationException(
                $"Unsupported file type '{request.ContentType}'. Allowed: PDF, JPEG, PNG, TIFF.");

        // 1. Build deterministic blob path: invoices/yyyy/MM/{newGuid}{ext}
        var ext = Path.GetExtension(request.OriginalFileName).ToLowerInvariant();
        var blobPath = $"invoices/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}{ext}";

        // 2. Upload to Azure Blob Storage
        await blobStorage.UploadAsync(request.FileStream, blobPath, request.ContentType, ct);

        // 3. Create Invoice entity (raises InvoiceUploadedEvent internally)
        var invoice = invoiceService.Create(
            request.VendorId,
            blobPath,
            request.OriginalFileName,
            request.FileSizeBytes,
            request.UploadedByUserId);

        // 4. Immediately transition to Extracting so the Functions app picks it up
        invoiceService.TransitionStatus(invoice, InvoiceStatus.Extracting);

        // 5. Audit record
        var audit = auditLogFactory.Create(
            action: "INVOICE_UPLOADED",
            invoiceId: invoice.Id,
            userId: request.UploadedByUserId,
            newValue: $"{{\"fileName\":\"{request.OriginalFileName}\",\"blobPath\":\"{blobPath}\"}}");

        db.Invoices.Add(invoice);
        db.AuditLogs.Add(audit);
        await db.SaveChangesAsync(ct);

        // 6. Publish to Service Bus — Azure Functions picks this up for AI extraction
        await serviceBus.PublishAsync(ExtractionQueue, new InvoiceExtractionMessage
        {
            InvoiceId = invoice.Id,
            BlobPath = blobPath,
            CorrelationId = Guid.NewGuid().ToString()
        }, ct);

        return new UploadInvoiceResponse(invoice.Id, blobPath, invoice.Status);
    }
}
