using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Enums;

namespace InvoiceFlow.Domain.Entities;

/// <summary>
/// A single approval workflow step — state and identity only.
/// All creation and mutation is handled by <see cref="Services.ApprovalStepDomainService"/>.
/// </summary>
public sealed class ApprovalStep : BaseEntity
{
    public Guid WorkflowId { get; internal set; }

    /// <summary>Invoice this step belongs to — denormalized for efficient queries.</summary>
    public Guid InvoiceId { get; internal set; }

    /// <summary>Position in the sequential approval chain (1-based).</summary>
    public int StepNumber { get; internal set; }

    public UserRole RequiredRole { get; internal set; }
    public Guid? AssignedToUserId { get; internal set; }
    public ApprovalStepStatus Status { get; internal set; } = ApprovalStepStatus.Pending;
    public string? Comment { get; internal set; }
    public DateTime? DecidedAt { get; internal set; }

    /// <summary>Original assignee if this step was delegated.</summary>
    public Guid? DelegatedFromUserId { get; internal set; }

    /// <summary>Timestamp of the 48-hour reminder notification.</summary>
    public DateTime? ReminderSentAt { get; internal set; }

    /// <summary>Timestamp when the step was escalated to the assigner's manager (72h).</summary>
    public DateTime? EscalatedAt { get; internal set; }

    // Navigation — populated by EF Core
    public ApprovalWorkflow? Workflow { get; private set; }

    internal ApprovalStep() { } // EF Core + ApprovalStepDomainService
}
