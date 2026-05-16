namespace InvoiceFlow.Domain.Enums;

/// <summary>Trust level of a vendor in the system.</summary>
public enum VendorStatus
{
    /// <summary>Registered and active. Normal processing applies.</summary>
    Active = 0,

    /// <summary>Explicitly trusted vendor. May receive expedited processing in future rules.</summary>
    Whitelisted = 1,

    /// <summary>Blocked vendor. Invoice uploads from this vendor are rejected at upload time.</summary>
    Blacklisted = 2
}
