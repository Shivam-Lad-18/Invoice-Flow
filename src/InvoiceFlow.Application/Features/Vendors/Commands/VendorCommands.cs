using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Enums;
using InvoiceFlow.Domain.Services;
using MediatR;

namespace InvoiceFlow.Application.Features.Vendors.Commands;

// ── Create ────────────────────────────────────────────────────────────────────

public sealed record CreateVendorCommand(
    string Name,
    string Email,
    string? TaxId) : IRequest<CreateVendorResponse>;

public sealed record CreateVendorResponse(
    Guid VendorId,
    string Name,
    string Email,
    VendorStatus Status);

internal sealed class CreateVendorCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    VendorDomainService vendorService) : IRequestHandler<CreateVendorCommand, CreateVendorResponse>
{
    public async Task<CreateVendorResponse> Handle(CreateVendorCommand request, CancellationToken ct)
    {
        var registeredBy = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User must be authenticated.");

        var vendor = vendorService.Create(
            request.Name,
            request.Email,
            request.TaxId,
            registeredBy);

        db.Vendors.Add(vendor);
        await db.SaveChangesAsync(ct);

        return new CreateVendorResponse(vendor.Id, vendor.Name, vendor.Email, vendor.Status);
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

public sealed record UpdateVendorCommand(
    Guid VendorId,
    string Name,
    string Email,
    string? TaxId) : IRequest;

internal sealed class UpdateVendorCommandHandler(
    IApplicationDbContext db,
    VendorDomainService vendorService) : IRequestHandler<UpdateVendorCommand>
{
    public async Task Handle(UpdateVendorCommand request, CancellationToken ct)
    {
        var vendor = await db.Vendors.FindAsync([request.VendorId], ct)
            ?? throw new KeyNotFoundException($"Vendor {request.VendorId} not found.");

        vendorService.Update(vendor, request.Name, request.Email, request.TaxId);
        await db.SaveChangesAsync(ct);
    }
}

// ── Set Status ────────────────────────────────────────────────────────────────

public sealed record SetVendorStatusCommand(
    Guid VendorId,
    VendorStatus Status) : IRequest;

internal sealed class SetVendorStatusCommandHandler(
    IApplicationDbContext db,
    VendorDomainService vendorService) : IRequestHandler<SetVendorStatusCommand>
{
    public async Task Handle(SetVendorStatusCommand request, CancellationToken ct)
    {
        var vendor = await db.Vendors.FindAsync([request.VendorId], ct)
            ?? throw new KeyNotFoundException($"Vendor {request.VendorId} not found.");

        vendorService.SetStatus(vendor, request.Status);
        await db.SaveChangesAsync(ct);
    }
}
