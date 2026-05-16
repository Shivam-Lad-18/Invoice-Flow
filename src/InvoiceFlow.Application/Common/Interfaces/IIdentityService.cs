using InvoiceFlow.Application.Common.Models;

namespace InvoiceFlow.Application.Common.Interfaces;

/// <summary>
/// Abstracts ASP.NET Core Identity user operations from the Application layer.
/// Implemented in Infrastructure using UserManager&lt;ApplicationUser&gt;.
/// </summary>
public interface IIdentityService
{
    /// <summary>Finds a user by email and returns their token info, or null if not found.</summary>
    Task<UserTokenInfo?> GetUserByEmailAsync(string email);

    /// <summary>Finds a user by ID and returns their token info, or null if not found.</summary>
    Task<UserTokenInfo?> GetUserByIdAsync(Guid userId);

    /// <summary>Validates a user's password. Returns false if user not found or password wrong.</summary>
    Task<bool> CheckPasswordAsync(Guid userId, string password);
}
