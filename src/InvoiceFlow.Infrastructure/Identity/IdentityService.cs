using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Application.Common.Models;
using InvoiceFlow.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace InvoiceFlow.Infrastructure.Identity;

/// <summary>
/// Wraps ASP.NET Core Identity's UserManager to keep Infrastructure details
/// out of the Application layer.
/// </summary>
public sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    /// <inheritdoc/>
    public async Task<UserTokenInfo?> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null || !user.IsActive ? null : ToTokenInfo(user);
    }

    /// <inheritdoc/>
    public async Task<UserTokenInfo?> GetUserByIdAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null || !user.IsActive ? null : ToTokenInfo(user);
    }

    /// <inheritdoc/>
    public async Task<bool> CheckPasswordAsync(Guid userId, string password)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;
        return await userManager.CheckPasswordAsync(user, password);
    }

    private static UserTokenInfo ToTokenInfo(ApplicationUser user) => new()
    {
        UserId = user.Id,
        Email = user.Email!,
        Role = user.Role,
        FullName = user.FirstName is not null && user.LastName is not null
            ? $"{user.FirstName} {user.LastName}"
            : null
    };
}
