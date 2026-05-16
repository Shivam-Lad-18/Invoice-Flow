using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Application.Features.Invoices.Queries;

// ── Request ───────────────────────────────────────────────────────────────────

public sealed record GetInvoiceByIdQuery(Guid InvoiceId) : IRequest<InvoiceDetailDto?>;

// ── Response DTOs ─────────────────────────────────────────────────────────────

public sealed record InvoiceDetailDto(
    Guid Id,
    string OriginalFileName,
    string BlobPath,
    long FileSizeBytes,
    InvoiceStatus Status,
    Guid VendorId,
    string? VendorName,
    DateTime UploadedAt,
    string? DuplicateCheckHash,
    ExtractionResultDto? ExtractionResult,
    ApprovalWorkflowDto? ApprovalWorkflow);

public sealed record ExtractionResultDto(
    Guid Id,
    string? VendorName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    decimal? TotalAmount,
    decimal? SubTotal,
    decimal? TaxAmount,
    string? Currency,
    bool HasLowConfidenceFields,
    bool IsManuallyCorrected,
    DateTime ExtractedAt);

public sealed record ApprovalWorkflowDto(
    Guid Id,
    int CurrentStepNumber,
    int TotalSteps,
    bool IsCompleted,
    DateTime? CompletedAt,
    IReadOnlyList<ApprovalStepDto> Steps);

public sealed record ApprovalStepDto(
    Guid Id,
    int StepNumber,
    UserRole RequiredRole,
    Guid? AssignedToUserId,
    ApprovalStepStatus Status,
    string? Comment,
    DateTime? DecidedAt);

// ── Handler ───────────────────────────────────────────────────────────────────

internal sealed class GetInvoiceByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetailDto?>
{
    public async Task<InvoiceDetailDto?> Handle(GetInvoiceByIdQuery request, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.Vendor)
            .Include(i => i.ExtractionResult)
            .Include(i => i.ApprovalWorkflow)
                .ThenInclude(w => w!.Steps)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct);

        if (invoice is null) return null;

        // Vendors can only see their own invoices
        if (currentUser.Role == UserRole.Vendor.ToString()
            && invoice.UploadedByUserId != currentUser.UserId)
            return null;

        var extractionDto = invoice.ExtractionResult is { } er
            ? new ExtractionResultDto(er.Id, er.VendorName, er.InvoiceNumber, er.InvoiceDate,
                er.DueDate, er.TotalAmount, er.SubTotal, er.TaxAmount, er.Currency,
                er.HasLowConfidenceFields, er.IsManuallyCorrected, er.ExtractedAt)
            : null;

        var workflowDto = invoice.ApprovalWorkflow is { } wf
            ? new ApprovalWorkflowDto(
                wf.Id, wf.CurrentStepNumber, wf.TotalSteps, wf.IsCompleted, wf.CompletedAt,
                wf.Steps.OrderBy(s => s.StepNumber)
                    .Select(s => new ApprovalStepDto(
                        s.Id, s.StepNumber, s.RequiredRole, s.AssignedToUserId,
                        s.Status, s.Comment, s.DecidedAt))
                    .ToList())
            : null;

        return new InvoiceDetailDto(
            invoice.Id, invoice.OriginalFileName, invoice.BlobPath, invoice.FileSizeBytes,
            invoice.Status, invoice.VendorId, invoice.Vendor?.Name,
            invoice.UploadedAt, invoice.DuplicateCheckHash,
            extractionDto, workflowDto);
    }
}
