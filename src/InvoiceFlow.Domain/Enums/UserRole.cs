namespace InvoiceFlow.Domain.Enums;

/// <summary>
/// Application roles controlling access and approval routing.
/// Stored as a claim in the JWT and persisted per user.
/// </summary>
public enum UserRole
{
    /// <summary>External vendor — can only upload invoices and view their own.</summary>
    Vendor = 0,

    /// <summary>Internal employee — can upload and correct extracted data.</summary>
    Employee = 1,

    /// <summary>Department manager — first approver in all workflows.</summary>
    Manager = 2,

    /// <summary>Finance head — second approver for invoices above ₹50,000.</summary>
    FinanceHead = 3,

    /// <summary>CFO — final approver for invoices above ₹5,00,000.</summary>
    CFO = 4,

    /// <summary>System administrator — full access to all resources and admin panel.</summary>
    Admin = 5
}
