using InvoiceFlow.Application.Common.Models;

namespace InvoiceFlow.Application.Common.Interfaces;

/// <summary>
/// Issues and validates JWT access tokens and refresh tokens.
/// Implemented in Infrastructure using Microsoft.IdentityModel.Tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Generates a signed JWT access token with user claims. Expires in 8 hours.</summary>
    string GenerateAccessToken(UserTokenInfo user);

    /// <summary>Generates a cryptographically random Base64Url refresh token string.</summary>
    string GenerateRefreshToken();
}
