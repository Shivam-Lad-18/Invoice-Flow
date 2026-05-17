using InvoiceFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InvoiceFlow.Infrastructure.Identity;

/// <summary>
/// Resolves the current authenticated user from JWT claims in the HTTP context.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    // JWT middleware maps "sub" → ClaimTypes.NameIdentifier by default
    public Guid? UserId => Guid.TryParse(
        User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public Guid? VendorId => Guid.TryParse(
        User?.FindFirstValue("vendor_id"), out var vid) ? vid : null;

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
