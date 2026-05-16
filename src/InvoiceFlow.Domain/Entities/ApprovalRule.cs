namespace InvoiceFlow.Domain.Entities;

/// <summary>
/// Admin-configurable amount-threshold approval rule — state only.
/// All creation and mutation is handled by <see cref="Services.ApprovalRuleDomainService"/>.
/// Rules are evaluated in ascending MaxAmount order; the first matching rule wins.
/// </summary>
public sealed class ApprovalRule
{
    public int Id { get; internal set; }

    /// <summary>
    /// Upper bound (inclusive) of the invoice amount for this rule.
    /// Use decimal.MaxValue for the highest tier (no upper limit).
    /// </summary>
    public decimal MaxAmount { get; internal set; }

    /// <summary>
    /// JSON array of UserRole enum values defining the sequential approval chain.
    /// Example: [2, 3] = Manager then FinanceHead.
    /// </summary>
    public string RequiredRoles { get; internal set; } = "[]";

    public DateTime LastUpdatedAt { get; internal set; } = DateTime.UtcNow;
    public Guid LastUpdatedByUserId { get; internal set; }

    internal ApprovalRule() { } // EF Core + ApprovalRuleDomainService
}
