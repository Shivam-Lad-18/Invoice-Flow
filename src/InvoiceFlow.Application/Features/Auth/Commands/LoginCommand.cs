using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Application.Features.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    Guid UserId,
    string Email,
    string Role,
    Guid? VendorId);

internal sealed class LoginCommandHandler(
    IIdentityService identityService,
    IJwtTokenService jwtTokenService,
    IApplicationDbContext db,
    RefreshTokenDomainService refreshTokenService) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await identityService.GetUserByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var isValid = await identityService.CheckPasswordAsync(user.UserId, request.Password);
        if (!isValid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var rawRefreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshToken = refreshTokenService.Create(user.UserId, rawRefreshToken, expiryDays: 7);

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: rawRefreshToken,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(480),
            UserId: user.UserId,
            Email: user.Email,
            Role: user.Role.ToString(),
            VendorId: user.VendorId);
    }
}
