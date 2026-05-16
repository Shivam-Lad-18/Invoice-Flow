namespace InvoiceFlow.Domain.Entities;

/// <summary>
/// Persisted refresh token — state only.
/// All creation and revocation is handled by <see cref="Services.RefreshTokenDomainService"/>.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; internal set; }

    /// <summary>Cryptographically random token value (Base64Url).</summary>
    public string Token { get; internal set; } = string.Empty;

    public DateTime ExpiresAt { get; internal set; }
    public bool IsRevoked { get; internal set; }

    /// <summary>Reason for revocation, e.g. "Logout", "Rotated", "SecurityBreach".</summary>
    public string? RevokedReason { get; internal set; }

    /// <summary>The token that replaced this one when rotated on refresh.</summary>
    public string? ReplacedByToken { get; internal set; }

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Computed — derived from persisted state, no mutation
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    internal RefreshToken() { } // EF Core + RefreshTokenDomainService
}
