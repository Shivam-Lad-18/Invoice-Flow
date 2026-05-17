using InvoiceFlow.Application.Features.Users.Commands;
using InvoiceFlow.Application.Features.Users.Queries;
using InvoiceFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvoiceFlow.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns a paginated list of all users. Admin only.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetUsersResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] UserRole? role = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetUsersQuery(page, Math.Clamp(pageSize, 1, 100), role, search), ct);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new user account with the given role. Admin only.
    /// For Vendor-role users, a VendorId must be provided to link to an existing Vendor entity.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required." });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Password is required." });

        try
        {
            var result = await mediator.Send(new CreateUserCommand(
                request.Email.Trim(),
                request.Password,
                request.FirstName?.Trim(),
                request.LastName?.Trim(),
                request.Role,
                request.VendorId), ct);

            return CreatedAtAction(nameof(GetAll), result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    UserRole Role,
    Guid? VendorId);
