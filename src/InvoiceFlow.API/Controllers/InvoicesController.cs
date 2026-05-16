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
public sealed class InvoicesController(IMediator mediator) : ControllerBase
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

        try
        {
            await using var stream = request.File.OpenReadStream();
            var response = await mediator.Send(new UploadInvoiceCommand(
                VendorId: request.VendorId,
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
    /// Returns full invoice detail including extraction result and approval workflow.
    /// Vendors can only access their own invoices.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInvoiceByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

// ── Request DTO ───────────────────────────────────────────────────────────────

public sealed class UploadInvoiceRequest
{
    public IFormFile? File { get; set; }
    public Guid VendorId { get; set; }
}
