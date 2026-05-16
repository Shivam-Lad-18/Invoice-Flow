using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Application.Features.Auth.Commands;

public sealed record LogoutCommand(string RefreshToken) : IRequest;

internal sealed class LogoutCommandHandler(
    IApplicationDbContext db,
    RefreshTokenDomainService refreshTokenService) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, ct);

        if (token is not null && token.IsActive)
        {
            refreshTokenService.Revoke(token, reason: "Logout");
            await db.SaveChangesAsync(ct);
        }
        // Silently succeed even if the token doesn't exist (idempotent logout)
    }
}
