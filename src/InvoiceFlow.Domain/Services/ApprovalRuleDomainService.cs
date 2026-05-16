using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Domain.Enums;

namespace InvoiceFlow.Domain.Services;

/// <summary>
/// Handles creation and admin-triggered updates of <see cref="ApprovalRule"/>.
/// Rules define which approval roles are required based on invoice amount thresholds.
/// </summary>
public sealed class ApprovalRuleDomainService
{
    /// <summary>
    /// Creates a new approval rule (used for seeding and admin setup).
    /// <paramref name="requiredRolesJson"/> is a JSON int array of <see cref="UserRole"/> values,
    /// e.g. "[2,3]" = Manager then FinanceHead.
    /// </summary>
    public ApprovalRule Create(int id, decimal maxAmount, string requiredRolesJson, Guid updatedByUserId, DateTime? lastUpdatedAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredRolesJson);

        return new ApprovalRule
        {
            Id = id,
            MaxAmount = maxAmount,
            RequiredRoles = requiredRolesJson,
            LastUpdatedByUserId = updatedByUserId,
            LastUpdatedAt = lastUpdatedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>Updates the amount threshold and role chain for an existing rule.</summary>
    public void Update(ApprovalRule rule, decimal maxAmount, string requiredRolesJson, Guid updatedByUserId)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredRolesJson);

        rule.MaxAmount = maxAmount;
        rule.RequiredRoles = requiredRolesJson;
        rule.LastUpdatedByUserId = updatedByUserId;
        rule.LastUpdatedAt = DateTime.UtcNow;
    }
}
