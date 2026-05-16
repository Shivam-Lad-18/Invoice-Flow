namespace InvoiceFlow.Domain.Enums;

/// <summary>Represents the status of a single step in the approval workflow.</summary>
public enum ApprovalStepStatus
{
    /// <summary>Awaiting action from the assigned approver.</summary>
    Pending = 0,

    /// <summary>Approver approved the invoice at this step.</summary>
    Approved = 1,

    /// <summary>Approver rejected the invoice at this step. Terminates the workflow.</summary>
    Rejected = 2,

    /// <summary>Approver delegated this step to another eligible user. Original step is closed.</summary>
    Delegated = 3,

    /// <summary>Step was skipped (e.g. approver is the same user as the previous step).</summary>
    Skipped = 4
}
