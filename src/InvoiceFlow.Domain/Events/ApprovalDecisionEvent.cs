using InvoiceFlow.Domain.Common;
using InvoiceFlow.Domain.Enums;

namespace InvoiceFlow.Domain.Events;

/// <summary>
/// Raised when an approver takes a decision (approve / reject / delegate) on a workflow step.
/// Triggers invoice status updates and SignalR notifications.
/// </summary>
public sealed record ApprovalDecisionEvent(
    Guid WorkflowId,
    Guid InvoiceId,
    Guid StepId,
    ApprovalStepStatus Decision,
    Guid? DecidedByUserId) : IDomainEvent;
