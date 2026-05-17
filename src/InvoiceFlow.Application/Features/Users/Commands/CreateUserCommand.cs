using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Enums;
using MediatR;

namespace InvoiceFlow.Application.Features.Users.Commands;

// ── Request ───────────────────────────────────────────────────────────────────

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    UserRole Role,
    Guid? VendorId) : IRequest<CreateUserResponse>;

public sealed record CreateUserResponse(
    Guid UserId,
    string Email,
    UserRole Role,
    Guid? VendorId);

// ── Handler ───────────────────────────────────────────────────────────────────

internal sealed class CreateUserCommandHandler(IIdentityService identityService)
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken ct)
    {
        if (request.Role == UserRole.Vendor && !request.VendorId.HasValue)
            throw new InvalidOperationException("A VendorId is required when creating a Vendor-role user.");

        return await identityService.CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Role,
            request.VendorId,
            ct);
    }
}
