namespace InvoiceFlow.Domain.Entities;

/// <summary>
/// Immutable, append-only audit record.
/// Every state change, approval decision, admin action, and correction is recorded here.
/// The database user must NOT have UPDATE or DELETE permissions on this table.
/// Created exclusively via <see cref="Services.AuditLogFactory"/>.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? InvoiceId { get; private set; }
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Short action identifier, e.g. "INVOICE_UPLOADED", "STEP_APPROVED", "VENDOR_BLACKLISTED".
    /// </summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>JSON snapshot of the entity state before the change. Null for creates.</summary>
    public string? OldValue { get; private set; }

    /// <summary>JSON snapshot of the entity state after the change. Null for deletes.</summary>
    public string? NewValue { get; private set; }

    /// <summary>Client IP address at the time of the action.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Correlation ID from the HTTP request for distributed tracing.</summary>
    public string? CorrelationId { get; private set; }

    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    // Navigation — populated by EF Core
    public Invoice? Invoice { get; private set; }

    private AuditLog() { } // EF Core

    // Internal constructor used exclusively by AuditLogFactory (same assembly)
    internal AuditLog(
        string action,
        Guid? invoiceId,
        Guid? userId,
        string? oldValue,
        string? newValue,
        string? ipAddress,
        string? correlationId)
    {
        Action = action;
        InvoiceId = invoiceId;
        UserId = userId;
        OldValue = oldValue;
        NewValue = newValue;
        IpAddress = ipAddress;
        CorrelationId = correlationId;
    }
}
