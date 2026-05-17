using InvoiceFlow.Application.Common.Interfaces;
using InvoiceFlow.Application.Features.Invoices.Commands;
using InvoiceFlow.Application.Features.Invoices.Queries;
using InvoiceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public sealed class InvoicesController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif"];
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    /// <summary>
    /// Uploads an invoice file. Accepted formats: PDF, JPEG, PNG, TIFF. Max 20 MB.
    /// The file is stored in Azure Blob Storage and queued for AI extraction automatically.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadInvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadInvoiceRequest request,
        CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { message = $"Unsupported file type '{ext}'. Allowed: {string.Join(", ", AllowedExtensions)}" });

        if (request.File.Length > MaxFileSizeBytes)
            return BadRequest(new { message = $"File exceeds the 20 MB limit." });

        var uploadedByUserId = GetCurrentUserId();
        if (uploadedByUserId == Guid.Empty)
            return Unauthorized();

        // Security: Vendor-role users can only upload for their own linked vendor.
        // Override whatever vendorId was sent in the form with the one from their JWT claim.
        var effectiveVendorId = request.VendorId;
        if (currentUser.Role == nameof(UserRole.Vendor))
        {
            if (!currentUser.VendorId.HasValue)
                return BadRequest(new { message = "Your account is not linked to a vendor. Contact your administrator." });
            effectiveVendorId = currentUser.VendorId.Value;
        }

        try
        {
            await using var stream = request.File.OpenReadStream();
            var response = await mediator.Send(new UploadInvoiceCommand(
                VendorId: effectiveVendorId,
                FileStream: stream,
                OriginalFileName: request.File.FileName,
                ContentType: request.File.ContentType,
                FileSizeBytes: request.File.Length,
                UploadedByUserId: uploadedByUserId), ct);

            return CreatedAtAction(nameof(GetById), new { id = response.InvoiceId }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Returns a paginated list of invoices. Vendors see only their own uploads.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetInvoicesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] InvoiceStatus? status = null,
        [FromQuery] Guid? vendorId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var response = await mediator.Send(
            new GetInvoicesQuery(page, Math.Clamp(pageSize, 1, 100), status, vendorId, from, to), ct);
        return Ok(response);
    }

    /// <summary>
    /// Returns full invoice detail including extraction result, line items, confidence scores,
    /// and approval workflow. Vendors can only access their own invoices.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoiceByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Returns a short-lived SAS URL (30 min) to view or download the original invoice file.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(InvoiceDownloadUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDownloadUrl(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoiceDownloadUrlQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Returns invoice counts grouped by status and total approved value. Used for dashboard.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(InvoiceStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoiceStatsQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the full audit trail for an invoice. Not available to Vendors.
    /// </summary>
    [HttpGet("{id:guid}/audit")]
    [Authorize(Roles = "Employee,Manager,FinanceHead,Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLogs(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoiceAuditLogsQuery(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Submits an extracted invoice for approval. Transitions status to PendingApproval
    /// and creates an ApprovalWorkflow based on configured approval rules.
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Employee,Manager,FinanceHead,Admin")]
    [ProducesResponseType(typeof(SubmitForApprovalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var result = await mediator.Send(new SubmitForApprovalCommand(id, userId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Approves the current pending step in the invoice's approval workflow.
    /// If this is the last step, the invoice is marked Approved.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager,FinanceHead,CFO,Admin")]
    [ProducesResponseType(typeof(ApproveInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var result = await mediator.Send(new ApproveInvoiceCommand(id, userId, request.Comment, currentUser.Role!), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Rejects the invoice at the current approval step. Requires a non-empty reason.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Manager,FinanceHead,CFO,Admin")]
    [ProducesResponseType(typeof(RejectInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Rejection reason is required." });

        try
        {
            var result = await mediator.Send(new RejectInvoiceCommand(id, userId, request.Reason, currentUser.Role!), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Manually corrects AI-extracted fields on an invoice in Extracted or PendingApproval status.
    /// Available to Employee, Manager, FinanceHead, Admin.
    /// </summary>
    [HttpPut("{id:guid}/extraction")]
    [Authorize(Roles = "Employee,Manager,FinanceHead,Admin")]
    [ProducesResponseType(typeof(CorrectExtractionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CorrectExtraction(
        Guid id, [FromBody] CorrectExtractionRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return Unauthorized();

        try
        {
            var result = await mediator.Send(new CorrectExtractionCommand(
                id, userId,
                request.VendorName, request.InvoiceNumber,
                request.InvoiceDate, request.DueDate,
                request.TotalAmount, request.SubTotal, request.TaxAmount,
                request.Currency), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed class UploadInvoiceRequest
{
    public IFormFile? File { get; set; }
    public Guid VendorId { get; set; }
}

public sealed record ApproveRequest(string? Comment);

public sealed record RejectRequest(string Reason);

public sealed record CorrectExtractionRequest(
    string? VendorName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    decimal? TotalAmount,
    decimal? SubTotal,
    decimal? TaxAmount,
    string? Currency);
