using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Application.Features.Invoices.Queries;

// ── Request ───────────────────────────────────────────────────────────────────

public sealed record GetInvoicesQuery(
    int Page = 1,
    int PageSize = 20,
    InvoiceStatus? Status = null,
    Guid? VendorId = null,
    DateTime? From = null,
    DateTime? To = null) : IRequest<GetInvoicesResponse>;

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record GetInvoicesResponse(
    IReadOnlyList<InvoiceListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record InvoiceListItemDto(
    Guid Id,
    string OriginalFileName,
    long FileSizeBytes,
    InvoiceStatus Status,
    Guid VendorId,
    string? VendorName,
    DateTime UploadedAt,
    decimal? TotalAmount,
    string? Currency);

// ── Handler ───────────────────────────────────────────────────────────────────

internal sealed class GetInvoicesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<GetInvoicesQuery, GetInvoicesResponse>
{
    public async Task<GetInvoicesResponse> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        var query = db.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.ExtractionResult)
            .AsNoTracking()
            .AsQueryable();

        // Vendors can only see their own invoices
        if (currentUser.Role == UserRole.Vendor.ToString())
            query = query.Where(i => i.UploadedByUserId == currentUser.UserId);

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (request.VendorId.HasValue)
            query = query.Where(i => i.VendorId == request.VendorId.Value);

        if (request.From.HasValue)
            query = query.Where(i => i.UploadedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(i => i.UploadedAt <= request.To.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.UploadedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceListItemDto(
                i.Id,
                i.OriginalFileName,
                i.FileSizeBytes,
                i.Status,
                i.VendorId,
                i.Vendor != null ? i.Vendor.Name : null,
                i.UploadedAt,
                i.ExtractionResult != null ? i.ExtractionResult.TotalAmount : null,
                i.ExtractionResult != null ? i.ExtractionResult.Currency : null))
            .ToListAsync(ct);

        return new GetInvoicesResponse(items, totalCount, request.Page, request.PageSize);
    }
}
