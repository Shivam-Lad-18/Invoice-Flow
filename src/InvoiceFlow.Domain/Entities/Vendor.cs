using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Enums;

namespace InvoiceFlow.Domain.Entities;

/// <summary>
/// Vendor identity and status — state only.
/// All creation and mutation is handled by <see cref="Services.VendorDomainService"/>.
/// </summary>
public sealed class Vendor : BaseEntity
{
    public string Name { get; internal set; } = string.Empty;
    public string Email { get; internal set; } = string.Empty;
    public string? TaxId { get; internal set; }
    public VendorStatus Status { get; internal set; } = VendorStatus.Active;
    public DateTime RegisteredAt { get; internal set; }
    public Guid RegisteredByUserId { get; internal set; }

    /// <summary>
    /// Optional link to an ApplicationUser account for portal login.
    /// Set by Admin after creating the vendor user account.
    /// </summary>
    public Guid? UserAccountId { get; internal set; }

    // Navigation — populated by EF Core
    public ICollection<Invoice> Invoices { get; private set; } = [];

    internal Vendor() { } // EF Core + VendorDomainService
}
