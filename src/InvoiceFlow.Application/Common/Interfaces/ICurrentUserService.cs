namespace InvoiceFlow.Application.Common.Interfaces;

/// <summary>
/// Provides the current authenticated user's context from the HTTP request.
/// Resolved from JWT claims by the Infrastructure implementation.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    /// <summary>Populated only for Vendor-role users. Comes from the vendor_id JWT claim.</summary>
    Guid? VendorId { get; }
    /// <summary>Client IP address from the HTTP context, used for audit logging.</summary>
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}
