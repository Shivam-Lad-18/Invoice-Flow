using InvoiceFlow.Domain.Entities;

namespace InvoiceFlow.Domain.Services;

/// <summary>
/// Creates immutable <see cref="AuditLog"/> entries.
/// AuditLog is truly append-only — no mutation methods exist.
/// The database user must NOT have UPDATE or DELETE on the AuditLogs table.
/// </summary>
public sealed class AuditLogFactory
{
    /// <summary>Creates a new audit log entry. Action string must be non-empty.</summary>
    public AuditLog Create(
        string action,
        Guid? invoiceId = null,
        Guid? userId = null,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return new AuditLog(action, invoiceId, userId, oldValue, newValue, ipAddress, correlationId);
    }
}
