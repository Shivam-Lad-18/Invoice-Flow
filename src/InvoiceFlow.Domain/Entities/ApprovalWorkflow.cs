using InvoiceFlow.Domain.Common;

namespace InvoiceFlow.Domain.Entities;

/// <summary>
/// Approval workflow state — step pointer and completion tracking only.
/// All creation and mutation is handled by <see cref="Services.ApprovalWorkflowDomainService"/>.
/// </summary>
public sealed class ApprovalWorkflow : BaseEntity
{
    public Guid InvoiceId { get; internal set; }

    /// <summary>The step number currently awaiting action (1-based).</summary>
    public int CurrentStepNumber { get; internal set; } = 1;

    public int TotalSteps { get; internal set; }
    public DateTime? CompletedAt { get; internal set; }

    // Computed read-only — derived from persisted state, no mutation
    public bool IsOnLastStep => CurrentStepNumber == TotalSteps;
    public bool IsCompleted => CompletedAt.HasValue;

    // Navigation — populated by EF Core
    public Invoice? Invoice { get; private set; }
    public ICollection<ApprovalStep> Steps { get; private set; } = [];

    internal ApprovalWorkflow() { } // EF Core + ApprovalWorkflowDomainService
}
