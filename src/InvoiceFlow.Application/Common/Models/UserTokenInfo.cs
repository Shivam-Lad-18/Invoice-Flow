using InvoiceFlow.Domain.Enums;

namespace InvoiceFlow.Application.Common.Models;

/// <summary>Minimal user information embedded in a JWT access token.</summary>
public sealed class UserTokenInfo
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required UserRole Role { get; init; }
    public string? FullName { get; init; }
    /// <summary>Populated only for Vendor-role accounts.</summary>
    public Guid? VendorId { get; init; }
}
