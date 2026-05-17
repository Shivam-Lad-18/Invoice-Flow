using FluentValidation;
using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Entities;
using InvoiceFlow.Domain.Enums;
using InvoiceFlow.Domain.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InvoiceFlow.Application.Features.Invoices.Commands;

// ══════════════════════════════════════════════════════════════════════════════
// SUBMIT FOR APPROVAL
// POST /api/invoices/{id}/submit
// Transitions Extracted → PendingApproval and creates the ApprovalWorkflow.
// ══════════════════════════════════════════════════════════════════════════════

public sealed record SubmitForApprovalCommand(Guid InvoiceId, Guid SubmittedByUserId)
    : IRequest<SubmitForApprovalResponse>;

public sealed record SubmitForApprovalResponse(
    Guid InvoiceId,
    Guid WorkflowId,
    int TotalSteps,
    InvoiceStatus Status);

internal sealed class SubmitForApprovalCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<SubmitForApprovalCommand, SubmitForApprovalResponse>
{
    public async Task<SubmitForApprovalResponse> Handle(
        SubmitForApprovalCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.ExtractionResult)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice {request.InvoiceId} not found.");

        if (invoice.Status != InvoiceStatus.Extracted)
            throw new InvalidOperationException(
                $"Invoice must be in Extracted status to submit for approval. Current: {invoice.Status}");

        // Load approval rules ordered by MaxAmount ascending; first match wins.
        var rules = await db.ApprovalRules
            .OrderBy(r => r.MaxAmount)
            .ToListAsync(ct);

        var totalAmount = invoice.ExtractionResult?.TotalAmount ?? 0m;
        var matchedRule = rules.FirstOrDefault(r => totalAmount <= r.MaxAmount);

        // Default: single Manager approval when no rules are configured.
        List<UserRole> requiredRoles = matchedRule is not null
            ? JsonSerializer.Deserialize<List<int>>(matchedRule.RequiredRoles)!
                .Select(i => (UserRole)i)
                .ToList()
            : [UserRole.Manager];

        // Create workflow
        var workflowService = new ApprovalWorkflowDomainService();
        var workflow = workflowService.Create(invoice.Id, requiredRoles.Count);

        var stepService = new ApprovalStepDomainService();
        var steps = requiredRoles.Select((role, idx) =>
            stepService.Create(workflow.Id, invoice.Id, idx + 1, role, null)).ToList();

        // Transition invoice
        var invoiceService = new InvoiceDomainService();
        invoiceService.TransitionStatus(invoice, InvoiceStatus.PendingApproval);

        // Audit
        var auditFactory = new AuditLogFactory();
        var audit = auditFactory.Create(
            action: "INVOICE_SUBMITTED",
            invoiceId: invoice.Id,
            userId: request.SubmittedByUserId,
            newValue: $"{{\"steps\":{requiredRoles.Count},\"totalAmount\":{totalAmount}}}");

        db.ApprovalWorkflows.Add(workflow);
        db.ApprovalSteps.AddRange(steps);
        db.AuditLogs.Add(audit);
        await db.SaveChangesAsync(ct);

        return new SubmitForApprovalResponse(invoice.Id, workflow.Id, requiredRoles.Count, invoice.Status);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// APPROVE CURRENT STEP
// POST /api/invoices/{id}/approve
// Approves the current workflow step. If it is the last step, transitions to Approved.
// ══════════════════════════════════════════════════════════════════════════════

public sealed record ApproveInvoiceCommand(Guid InvoiceId, Guid ApproverId, string? Comment)
    : IRequest<ApproveInvoiceResponse>;

public sealed record ApproveInvoiceResponse(
    Guid InvoiceId,
    InvoiceStatus Status,
    bool WorkflowComplete,
    int CurrentStep,
    int TotalSteps);

public sealed class ApproveInvoiceCommandValidator : AbstractValidator<ApproveInvoiceCommand>
{
    public ApproveInvoiceCommandValidator()
    {
        RuleFor(x => x.ApproverId).NotEmpty();
    }
}

internal sealed class ApproveInvoiceCommandHandler(
    IApplicationDbContext db) : IRequestHandler<ApproveInvoiceCommand, ApproveInvoiceResponse>
{
    public async Task<ApproveInvoiceResponse> Handle(
        ApproveInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.ApprovalWorkflow)
                .ThenInclude(w => w!.Steps)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice {request.InvoiceId} not found.");

        if (invoice.Status != InvoiceStatus.PendingApproval && invoice.Status != InvoiceStatus.InApproval)
            throw new InvalidOperationException(
                $"Invoice is not awaiting approval. Current status: {invoice.Status}");

        var workflow = invoice.ApprovalWorkflow
            ?? throw new InvalidOperationException("Invoice has no approval workflow.");

        var currentStep = workflow.Steps
            .OrderBy(s => s.StepNumber)
            .FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending)
            ?? throw new InvalidOperationException("No pending approval step found.");

        // Approve the step
        var stepService = new ApprovalStepDomainService();
        stepService.Approve(currentStep, request.Comment);

        // Advance invoice to InApproval (if not already) after first approval
        var invoiceService = new InvoiceDomainService();
        if (invoice.Status == InvoiceStatus.PendingApproval)
            invoiceService.TransitionStatus(invoice, InvoiceStatus.InApproval);

        // Advance or complete workflow
        var workflowService = new ApprovalWorkflowDomainService();
        var isLast = workflow.IsOnLastStep;

        if (isLast)
        {
            workflowService.Complete(workflow);
            invoiceService.TransitionStatus(invoice, InvoiceStatus.Approved);
        }
        else
        {
            workflowService.AdvanceToNextStep(workflow);
        }

        // Audit
        var auditFactory = new AuditLogFactory();
        db.AuditLogs.Add(auditFactory.Create(
            action: isLast ? "INVOICE_APPROVED_FINAL" : "INVOICE_STEP_APPROVED",
            invoiceId: invoice.Id,
            userId: request.ApproverId,
            newValue: $"{{\"step\":{currentStep.StepNumber},\"comment\":\"{request.Comment}\"}}"));

        await db.SaveChangesAsync(ct);

        return new ApproveInvoiceResponse(
            invoice.Id, invoice.Status, workflow.IsCompleted,
            workflow.CurrentStepNumber, workflow.TotalSteps);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// REJECT INVOICE
// POST /api/invoices/{id}/reject
// Rejects at the current step and transitions invoice to Rejected.
// ══════════════════════════════════════════════════════════════════════════════

public sealed record RejectInvoiceCommand(Guid InvoiceId, Guid RejecterId, string Reason)
    : IRequest<RejectInvoiceResponse>;

public sealed record RejectInvoiceResponse(Guid InvoiceId, InvoiceStatus Status);

public sealed class RejectInvoiceCommandValidator : AbstractValidator<RejectInvoiceCommand>
{
    public RejectInvoiceCommandValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000)
            .WithMessage("Rejection reason is required.");
    }
}

internal sealed class RejectInvoiceCommandHandler(
    IApplicationDbContext db) : IRequestHandler<RejectInvoiceCommand, RejectInvoiceResponse>
{
    public async Task<RejectInvoiceResponse> Handle(
        RejectInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.ApprovalWorkflow)
                .ThenInclude(w => w!.Steps)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice {request.InvoiceId} not found.");

        if (invoice.Status != InvoiceStatus.PendingApproval && invoice.Status != InvoiceStatus.InApproval)
            throw new InvalidOperationException(
                $"Invoice cannot be rejected in its current status: {invoice.Status}");

        // Reject the current pending step
        var pendingStep = invoice.ApprovalWorkflow?.Steps
            .OrderBy(s => s.StepNumber)
            .FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending);

        if (pendingStep is not null)
        {
            var stepService = new ApprovalStepDomainService();
            stepService.Reject(pendingStep, request.Reason);
        }

        // Transition invoice to Rejected
        var invoiceService = new InvoiceDomainService();
        invoiceService.TransitionStatus(invoice, InvoiceStatus.Rejected);

        var auditFactory = new AuditLogFactory();
        db.AuditLogs.Add(auditFactory.Create(
            action: "INVOICE_REJECTED",
            invoiceId: invoice.Id,
            userId: request.RejecterId,
            newValue: $"{{\"reason\":\"{request.Reason.Replace("\"", "\\\"")}\"}}"));

        await db.SaveChangesAsync(ct);

        return new RejectInvoiceResponse(invoice.Id, invoice.Status);
    }
}

// ══════════════════════════════════════════════════════════════════════════════
// CORRECT EXTRACTION
// PUT /api/invoices/{id}/extraction
// Allows an employee/manager to manually correct AI-extracted fields.
// ══════════════════════════════════════════════════════════════════════════════

public sealed record CorrectExtractionCommand(
    Guid InvoiceId,
    Guid CorrectedByUserId,
    string? VendorName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    decimal? TotalAmount,
    decimal? SubTotal,
    decimal? TaxAmount,
    string? Currency) : IRequest<CorrectExtractionResponse>;

public sealed record CorrectExtractionResponse(Guid ExtractionResultId, bool IsManuallyCorrected);

public sealed class CorrectExtractionCommandValidator : AbstractValidator<CorrectExtractionCommand>
{
    public CorrectExtractionCommandValidator()
    {
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0).When(x => x.TotalAmount.HasValue);
        RuleFor(x => x.SubTotal).GreaterThanOrEqualTo(0).When(x => x.SubTotal.HasValue);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).When(x => x.TaxAmount.HasValue);
        RuleFor(x => x.Currency).MaximumLength(10).When(x => x.Currency is not null);
        RuleFor(x => x.VendorName).MaximumLength(256).When(x => x.VendorName is not null);
        RuleFor(x => x.InvoiceNumber).MaximumLength(128).When(x => x.InvoiceNumber is not null);
    }
}

internal sealed class CorrectExtractionCommandHandler(
    IApplicationDbContext db) : IRequestHandler<CorrectExtractionCommand, CorrectExtractionResponse>
{
    public async Task<CorrectExtractionResponse> Handle(
        CorrectExtractionCommand request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.ExtractionResult)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct)
            ?? throw new InvalidOperationException($"Invoice {request.InvoiceId} not found.");

        if (invoice.ExtractionResult is null)
            throw new InvalidOperationException("Invoice has no extraction result to correct.");

        if (invoice.Status != InvoiceStatus.Extracted && invoice.Status != InvoiceStatus.PendingApproval)
            throw new InvalidOperationException(
                $"Extraction correction is not allowed in status: {invoice.Status}");

        var extractionService = new ExtractionResultDomainService();
        extractionService.ApplyManualCorrection(
            invoice.ExtractionResult,
            request.VendorName,
            request.InvoiceNumber,
            request.InvoiceDate,
            request.DueDate,
            request.TotalAmount,
            request.SubTotal,
            request.TaxAmount,
            request.Currency,
            request.CorrectedByUserId);

        var auditFactory = new AuditLogFactory();
        db.AuditLogs.Add(auditFactory.Create(
            action: "EXTRACTION_CORRECTED",
            invoiceId: invoice.Id,
            userId: request.CorrectedByUserId,
            newValue: $"{{\"invoiceNumber\":\"{request.InvoiceNumber}\",\"totalAmount\":{request.TotalAmount}}}"));

        await db.SaveChangesAsync(ct);

        return new CorrectExtractionResponse(invoice.ExtractionResult.Id, true);
    }
}
