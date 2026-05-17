using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Application.Common.Models;
using InvoiceFlow.Application.Features.Users.Commands;
using InvoiceFlow.Application.Features.Users.Queries;
using InvoiceFlow.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

    /// <inheritdoc/>
    public async Task<GetUsersResponse> GetUsersAsync(
        int page, int pageSize, UserRole? role, string? search, CancellationToken ct)
    {
        var query = userManager.Users.AsQueryable();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(s)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(s)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemDto(
                u.Id,
                u.Email!,
                u.FirstName != null && u.LastName != null ? u.FirstName + " " + u.LastName : null,
                u.Role,
                u.VendorId,
                null,   // VendorName — not joined here for performance; frontend can join if needed
                u.IsActive,
                u.CreatedAt))
            .ToListAsync(ct);

        return new GetUsersResponse(items, total, page, pageSize);
    }

    /// <inheritdoc/>
    public async Task<CreateUserResponse> CreateUserAsync(
        string email, string password,
        string? firstName, string? lastName,
        UserRole role, Guid? vendorId,
        CancellationToken ct)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            throw new InvalidOperationException($"A user with email '{email}' already exists.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            VendorId = vendorId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        return new CreateUserResponse(user.Id, user.Email!, user.Role, user.VendorId);
    }

    private static UserTokenInfo ToTokenInfo(ApplicationUser user) => new()
    {
        UserId = user.Id,
        Email = user.Email!,
        Role = user.Role,
        FullName = user.FirstName is not null && user.LastName is not null
            ? $"{user.FirstName} {user.LastName}"
            : null,
        VendorId = user.VendorId
    };
}
