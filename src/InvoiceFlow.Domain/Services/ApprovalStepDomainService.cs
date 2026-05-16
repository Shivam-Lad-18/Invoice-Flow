using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Domain.Enums;
using InvoiceFlow.Domain.Events;

namespace InvoiceFlow.Domain.Services;

/// <summary>
/// Handles all creation and state mutation for <see cref="ApprovalStep"/>.
/// Enforces that only Pending steps can be acted upon.
/// Raises <see cref="ApprovalDecisionEvent"/> on approve and reject.
/// </summary>
public sealed class ApprovalStepDomainService
{
    /// <summary>Creates a new approval step for a given workflow position.</summary>
    public ApprovalStep Create(
        Guid workflowId,
        Guid invoiceId,
        int stepNumber,
        UserRole requiredRole,
        Guid? assignedToUserId) => new()
        {
            WorkflowId = workflowId,
            InvoiceId = invoiceId,
            StepNumber = stepNumber,
            RequiredRole = requiredRole,
            AssignedToUserId = assignedToUserId
        };

    /// <summary>Approves this step with an optional comment.</summary>
    public void Approve(ApprovalStep step, string? comment)
    {
        ArgumentNullException.ThrowIfNull(step);
        EnsurePending(step);

        step.Status = ApprovalStepStatus.Approved;
        step.Comment = comment?.Trim();
        step.DecidedAt = DateTime.UtcNow;
        step.MarkUpdated();

        step.RaiseDomainEvent(new ApprovalDecisionEvent(
            step.WorkflowId, step.InvoiceId, step.Id,
            ApprovalStepStatus.Approved, step.AssignedToUserId));
    }

    /// <summary>Rejects this step. A non-empty rejection comment is mandatory.</summary>
    public void Reject(ApprovalStep step, string comment)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentException.ThrowIfNullOrWhiteSpace(comment);
        EnsurePending(step);

        step.Status = ApprovalStepStatus.Rejected;
        step.Comment = comment.Trim();
        step.DecidedAt = DateTime.UtcNow;
        step.MarkUpdated();

        step.RaiseDomainEvent(new ApprovalDecisionEvent(
            step.WorkflowId, step.InvoiceId, step.Id,
            ApprovalStepStatus.Rejected, step.AssignedToUserId));
    }

    /// <summary>
    /// Delegates this step to another eligible user.
    /// The step is marked Delegated; the caller must create a replacement step for the target.
    /// </summary>
    public void Delegate(ApprovalStep step, Guid delegatedToUserId)
    {
        ArgumentNullException.ThrowIfNull(step);
        EnsurePending(step);

        if (delegatedToUserId == step.AssignedToUserId)
            throw new InvalidOperationException("Cannot delegate a step to the same assignee.");

        step.DelegatedFromUserId = step.AssignedToUserId;
        step.AssignedToUserId = delegatedToUserId;
        step.Status = ApprovalStepStatus.Delegated;
        step.DecidedAt = DateTime.UtcNow;
        step.MarkUpdated();
    }

    /// <summary>Records that the 48-hour reminder notification was sent.</summary>
    public void MarkReminderSent(ApprovalStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        step.ReminderSentAt = DateTime.UtcNow;
        step.MarkUpdated();
    }

    /// <summary>Records that the 72-hour escalation was triggered.</summary>
    public void MarkEscalated(ApprovalStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        step.EscalatedAt = DateTime.UtcNow;
        step.MarkUpdated();
    }

    /// <summary>Reassigns the step to a new user during manager escalation.</summary>
    public void Reassign(ApprovalStep step, Guid newUserId)
    {
        ArgumentNullException.ThrowIfNull(step);
        EnsurePending(step);

        step.AssignedToUserId = newUserId;
        step.MarkUpdated();
    }

    private static void EnsurePending(ApprovalStep step)
    {
        if (step.Status != ApprovalStepStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot perform action on a step with status '{step.Status}'.");
    }
}
