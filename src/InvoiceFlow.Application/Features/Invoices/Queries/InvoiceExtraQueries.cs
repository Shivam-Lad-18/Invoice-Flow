using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Application.Features.Invoices.Queries;

// ══════════════════════════════════════════════════════════════════════════════
// GET INVOICE DOWNLOAD URL
// GET /api/invoices/{id}/download
// Returns a short-lived SAS URL to view the invoice file in the browser.
// ══════════════════════════════════════════════════════════════════════════════

public sealed record GetInvoiceDownloadUrlQuery(Guid InvoiceId) : IRequest<InvoiceDownloadUrlDto?>;

public sealed record InvoiceDownloadUrlDto(Guid InvoiceId, Uri DownloadUrl, DateTime ExpiresAt);

internal sealed class GetInvoiceDownloadUrlQueryHandler(
    IApplicationDbContext db,
    IBlobStorageService blobStorage,
    ICurrentUserService currentUser) : IRequestHandler<GetInvoiceDownloadUrlQuery, InvoiceDownloadUrlDto?>
{
    public async Task<InvoiceDownloadUrlDto?> Handle(
        GetInvoiceDownloadUrlQuery request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct);

        if (invoice is null) return null;

        // Vendors can only access their own invoices
        if (currentUser.Role == UserRole.Vendor.ToString()
            && invoice.UploadedByUserId != currentUser.UserId)
            return null;

        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var url = await blobStorage.GenerateSasUrlAsync(invoice.BlobPath, TimeSpan.FromMinutes(30), ct);

        return new InvoiceDownloadUrlDto(invoice.Id, url, expiresAt);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GET INVOICE STATS (Dashboard)
// GET /api/invoices/stats
// Returns invoice counts grouped by status, plus total value of approved invoices.
// ══════════════════════════════════════════════════════════════════════════════

public sealed record GetInvoiceStatsQuery : IRequest<InvoiceStatsDto>;

public sealed record InvoiceStatsDto(
    int Total,
    int Uploaded,
    int Extracting,
    int Extracted,
    int PendingApproval,
    int InApproval,
    int Approved,
    int Rejected,
    decimal ApprovedTotalValue);

internal sealed class GetInvoiceStatsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<GetInvoiceStatsQuery, InvoiceStatsDto>
{
    public async Task<InvoiceStatsDto> Handle(GetInvoiceStatsQuery request, CancellationToken ct)
    {
        var query = db.Invoices.AsNoTracking();

        // Vendors see only their own invoices
        if (currentUser.Role == UserRole.Vendor.ToString() && currentUser.UserId.HasValue)
            query = query.Where(i => i.UploadedByUserId == currentUser.UserId.Value);

        var counts = await query
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var approvedValue = await query
            .Where(i => i.Status == InvoiceStatus.Approved)
            .Join(db.ExtractionResults, i => i.Id, er => er.InvoiceId,
                (i, er) => er.TotalAmount)
            .SumAsync(a => a ?? 0m, ct);

        int Get(InvoiceStatus s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

        return new InvoiceStatsDto(
            Total: counts.Sum(c => c.Count),
            Uploaded: Get(InvoiceStatus.Uploaded),
            Extracting: Get(InvoiceStatus.Extracting),
            Extracted: Get(InvoiceStatus.Extracted),
            PendingApproval: Get(InvoiceStatus.PendingApproval),
            InApproval: Get(InvoiceStatus.InApproval),
            Approved: Get(InvoiceStatus.Approved),
            Rejected: Get(InvoiceStatus.Rejected),
            ApprovedTotalValue: approvedValue);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// GET AUDIT LOGS FOR INVOICE
// GET /api/invoices/{id}/audit
// Returns the full activity trail for an invoice.
// ══════════════════════════════════════════════════════════════════════════════

public sealed record GetInvoiceAuditLogsQuery(Guid InvoiceId) : IRequest<IReadOnlyList<AuditLogDto>>;

public sealed record AuditLogDto(
    Guid Id,
    string Action,
    Guid? UserId,
    string? OldValue,
    string? NewValue,
    DateTime Timestamp);

internal sealed class GetInvoiceAuditLogsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<GetInvoiceAuditLogsQuery, IReadOnlyList<AuditLogDto>>
{
    public async Task<IReadOnlyList<AuditLogDto>> Handle(
        GetInvoiceAuditLogsQuery request, CancellationToken ct)
    {
        // Vendors cannot view audit logs
        if (currentUser.Role == UserRole.Vendor.ToString())
            return [];

        return await db.AuditLogs
            .AsNoTracking()
            .Where(a => a.InvoiceId == request.InvoiceId)
            .OrderBy(a => a.Timestamp)
            .Select(a => new AuditLogDto(a.Id, a.Action, a.UserId, a.OldValue, a.NewValue, a.Timestamp))
            .ToListAsync(ct);
    }
}
