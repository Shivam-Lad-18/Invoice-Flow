using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Domain.Enums;

namespace InvoiceFlow.Domain.Services;

/// <summary>
/// Handles all creation and mutation for <see cref="Vendor"/>.
/// Admin is the only actor who can create or modify vendors.
/// </summary>
public sealed class VendorDomainService
{
    /// <summary>
    /// Creates a new vendor. Email is normalised to lowercase.
    /// </summary>
    public Vendor Create(string name, string email, string? taxId, Guid registeredByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new Vendor
        {
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            TaxId = taxId?.Trim(),
            RegisteredByUserId = registeredByUserId,
            RegisteredAt = DateTime.UtcNow
        };
    }

    /// <summary>Changes the vendor's trust status (Active / Whitelisted / Blacklisted).</summary>
    public void SetStatus(Vendor vendor, VendorStatus status)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        vendor.Status = status;
        vendor.MarkUpdated();
    }

    /// <summary>
    /// Links an ApplicationUser account to this vendor for portal access.
    /// Set after Admin creates the corresponding user account.
    /// </summary>
    public void LinkUserAccount(Vendor vendor, Guid userId)
    {
        ArgumentNullException.ThrowIfNull(vendor);

        vendor.UserAccountId = userId;
        vendor.MarkUpdated();
    }

    /// <summary>Updates the vendor's editable profile fields.</summary>
    public void Update(Vendor vendor, string name, string email, string? taxId)
    {
        ArgumentNullException.ThrowIfNull(vendor);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        vendor.Name = name.Trim();
        vendor.Email = email.Trim().ToLowerInvariant();
        vendor.TaxId = taxId?.Trim();
        vendor.MarkUpdated();
    }
}
