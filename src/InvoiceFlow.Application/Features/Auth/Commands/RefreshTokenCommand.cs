using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Application.Features.Auth.Commands;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;

internal sealed class RefreshTokenCommandHandler(
    IIdentityService identityService,
    IJwtTokenService jwtTokenService,
    IApplicationDbContext db,
    RefreshTokenDomainService refreshTokenService) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existing.IsActive)
            throw new UnauthorizedAccessException("Refresh token is expired or revoked.");

        var user = await identityService.GetUserByIdAsync(existing.UserId)
            ?? throw new UnauthorizedAccessException("User not found.");

        var newRawToken = jwtTokenService.GenerateRefreshToken();
        var newRefreshToken = refreshTokenService.Create(existing.UserId, newRawToken, expiryDays: 7);

        // Rotate: revoke the old token and record what replaced it
        refreshTokenService.Revoke(existing, reason: "Rotated", replacedByToken: newRawToken);

        db.RefreshTokens.Add(newRefreshToken);
        await db.SaveChangesAsync(ct);

        var accessToken = jwtTokenService.GenerateAccessToken(user);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: newRawToken,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(480),
            UserId: user.UserId,
            Email: user.Email,
            Role: user.Role.ToString(),
            VendorId: user.VendorId);
    }
}
