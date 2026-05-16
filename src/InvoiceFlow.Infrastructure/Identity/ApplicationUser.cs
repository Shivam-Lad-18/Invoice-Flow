using InvoiceFlow.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace InvoiceFlow.Infrastructure.Identity;

/// <summary>
/// Extends ASP.NET Core Identity's IdentityUser with InvoiceFlow-specific properties.
/// Lives in Infrastructure because it depends on Microsoft.AspNetCore.Identity.
/// Domain entities reference users by UserId (Guid) only — no navigation to this class.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public UserRole Role { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>Department identifier — used for manager resolution in approval routing.</summary>
    public string? DepartmentId { get; set; }

    /// <summary>
    /// Direct manager's UserId. Used by the approval engine to resolve the Manager step assignee.
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>Linked vendor record if this user is a Vendor role account.</summary>
    public Guid? VendorId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
