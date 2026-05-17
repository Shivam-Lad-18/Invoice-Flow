using InvoiceFlow.Application.Features.Vendors.Commands;
using InvoiceFlow.Application.Features.Vendors.Queries;
using InvoiceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.API.Controllers;

[ApiController]
[Route("api/vendors")]
[Authorize]
public sealed class VendorsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of vendors. Supports optional search and status filter.
    /// Accessible to all authenticated roles.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetVendorsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] VendorStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var response = await mediator.Send(
            new GetVendorsQuery(page, Math.Clamp(pageSize, 1, 100), status, search), ct);
        return Ok(response);
    }

    /// <summary>
    /// Returns full vendor detail by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VendorDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetVendorByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new vendor. Admin only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(typeof(CreateVendorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVendorRequest request,
        CancellationToken ct)
    {
        var response = await mediator.Send(
            new CreateVendorCommand(request.Name, request.Email, request.TaxId), ct);

        return CreatedAtAction(nameof(GetById), new { id = response.VendorId }, response);
    }

    /// <summary>
    /// Updates vendor profile (name, email, tax ID). Admin only.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVendorRequest request,
        CancellationToken ct)
    {
        try
        {
            await mediator.Send(new UpdateVendorCommand(id, request.Name, request.Email, request.TaxId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Changes vendor status (Active / Whitelisted / Blacklisted). Admin only.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus(
        Guid id,
        [FromBody] SetVendorStatusRequest request,
        CancellationToken ct)
    {
        try
        {
            await mediator.Send(new SetVendorStatusCommand(id, request.Status), ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record CreateVendorRequest(string Name, string Email, string? TaxId);
public sealed record UpdateVendorRequest(string Name, string Email, string? TaxId);
public sealed record SetVendorStatusRequest(VendorStatus Status);
