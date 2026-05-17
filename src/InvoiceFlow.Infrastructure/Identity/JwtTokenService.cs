using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Application.Common.Models;
using InvoiceFlow.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace InvoiceFlow.Infrastructure.Identity;

/// <summary>
/// Issues JWT access tokens and cryptographically random refresh token strings.
/// Reads settings from the "Jwt" configuration section.
/// </summary>
public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private readonly string _secret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "InvoiceFlow";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "InvoiceFlow-Client";
    private readonly int _expiryMinutes = int.TryParse(configuration["Jwt:AccessTokenExpiryMinutes"], out var m) ? m : 480;

    /// <inheritdoc/>
    public string GenerateAccessToken(UserTokenInfo user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role_id", ((int)user.Role).ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (user.FullName is not null)
            claims.Add(new Claim("full_name", user.FullName));

        if (user.VendorId.HasValue)
            claims.Add(new Claim("vendor_id", user.VendorId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc/>
    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
