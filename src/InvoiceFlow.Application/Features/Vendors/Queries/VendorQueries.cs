using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Application.Features.Vendors.Queries;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record VendorDto(
    Guid Id,
    string Name,
    string Email,
    string? TaxId,
    VendorStatus Status,
    DateTime RegisteredAt,
    int InvoiceCount);

public sealed record VendorDetailDto(
    Guid Id,
    string Name,
    string Email,
    string? TaxId,
    VendorStatus Status,
    DateTime RegisteredAt,
    Guid RegisteredByUserId,
    Guid? UserAccountId,
    int InvoiceCount);

// ── Get list ──────────────────────────────────────────────────────────────────

public sealed record GetVendorsQuery(
    int Page = 1,
    int PageSize = 20,
    VendorStatus? Status = null,
    string? Search = null) : IRequest<GetVendorsResponse>;

public sealed record GetVendorsResponse(
    IReadOnlyList<VendorDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

internal sealed class GetVendorsQueryHandler(
    IApplicationDbContext db) : IRequestHandler<GetVendorsQuery, GetVendorsResponse>
{
    public async Task<GetVendorsResponse> Handle(GetVendorsQuery request, CancellationToken ct)
    {
        var query = db.Vendors.AsNoTracking().AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(v => v.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.ToLower();
            query = query.Where(v =>
                v.Name.ToLower().Contains(term) ||
                v.Email.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(v => v.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VendorDto(
                v.Id,
                v.Name,
                v.Email,
                v.TaxId,
                v.Status,
                v.RegisteredAt,
                v.Invoices.Count))
            .ToListAsync(ct);

        return new GetVendorsResponse(items, totalCount, request.Page, request.PageSize);
    }
}

// ── Get by ID ─────────────────────────────────────────────────────────────────

public sealed record GetVendorByIdQuery(Guid VendorId) : IRequest<VendorDetailDto?>;

internal sealed class GetVendorByIdQueryHandler(
    IApplicationDbContext db) : IRequestHandler<GetVendorByIdQuery, VendorDetailDto?>
{
    public async Task<VendorDetailDto?> Handle(GetVendorByIdQuery request, CancellationToken ct)
    {
        return await db.Vendors
            .AsNoTracking()
            .Where(v => v.Id == request.VendorId)
            .Select(v => new VendorDetailDto(
                v.Id,
                v.Name,
                v.Email,
                v.TaxId,
                v.Status,
                v.RegisteredAt,
                v.RegisteredByUserId,
                v.UserAccountId,
                v.Invoices.Count))
            .FirstOrDefaultAsync(ct);
    }
}
