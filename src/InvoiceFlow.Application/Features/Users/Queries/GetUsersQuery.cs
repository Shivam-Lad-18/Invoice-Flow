using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Enums;
using MediatR;

namespace InvoiceFlow.Application.Features.Users.Queries;

// ── Request ───────────────────────────────────────────────────────────────────

public sealed record GetUsersQuery(
    int Page = 1,
    int PageSize = 20,
    UserRole? Role = null,
    string? Search = null) : IRequest<GetUsersResponse>;

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record GetUsersResponse(
    IReadOnlyList<UserListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record UserListItemDto(
    Guid UserId,
    string Email,
    string? FullName,
    UserRole Role,
    Guid? VendorId,
    string? VendorName,
    bool IsActive,
    DateTime CreatedAt);

// ── Handler ───────────────────────────────────────────────────────────────────

internal sealed class GetUsersQueryHandler(IIdentityService identityService)
    : IRequestHandler<GetUsersQuery, GetUsersResponse>
{
    public async Task<GetUsersResponse> Handle(GetUsersQuery request, CancellationToken ct)
    {
        return await identityService.GetUsersAsync(
            request.Page, request.PageSize, request.Role, request.Search, ct);
    }
}
