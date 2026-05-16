using InvoiceFlow.Domain.Entities;

namespace InvoiceFlow.Domain.Services;

/// <summary>
/// Handles creation and step progression for <see cref="ApprovalWorkflow"/>.
/// The workflow pointer and completion state are the only mutable concerns here.
/// </summary>
public sealed class ApprovalWorkflowDomainService
{
    /// <summary>Creates a new approval workflow for an invoice with the given number of steps.</summary>
    public ApprovalWorkflow Create(Guid invoiceId, int totalSteps)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(totalSteps, 1);

        return new ApprovalWorkflow
        {
            InvoiceId = invoiceId,
            TotalSteps = totalSteps,
            CurrentStepNumber = 1
        };
    }

    /// <summary>
    /// Advances the workflow pointer to the next step after a step is approved.
    /// No-op if already on the last step.
    /// </summary>
    public void AdvanceToNextStep(ApprovalWorkflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        if (workflow.CurrentStepNumber < workflow.TotalSteps)
        {
            workflow.CurrentStepNumber++;
            workflow.MarkUpdated();
        }
    }

    /// <summary>Marks the workflow as fully completed when all steps are approved.</summary>
    public void Complete(ApprovalWorkflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        workflow.CompletedAt = DateTime.UtcNow;
        workflow.MarkUpdated();
    }
}
