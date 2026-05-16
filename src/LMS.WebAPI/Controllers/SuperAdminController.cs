using FluentValidation;
using LMS.Application.Common;
using LMS.Application.SuperAdmin.Commands;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Application.SuperAdmin.Queries;
using LMS.Domain.Constants;
using LMS.WebAPI.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Platform-wide endpoints reserved for the SuperAdmin role: organization and
/// branch CRUD plus seeding the first OrgAdmin per organization. Permission
/// claims (org.*, branch.*, orgadmin.*) gate the individual actions so we can
/// later loosen them to OrgAdmins for the org-scoped pieces.
/// </summary>
[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class SuperAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuperAdminController(IMediator mediator) { _mediator = mediator; }

    // ------------------------------------------------------------------
    // Organizations
    // ------------------------------------------------------------------

    [HttpGet("organizations")]
    [RequirePermission(Permissions.OrgRead)]
    [ProducesResponseType(typeof(PagedResult<OrganizationListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrganizationListItemDto>>> GetOrganizations(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetOrganizationsQuery(search, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("organizations/{organizationId:guid}")]
    [RequirePermission(Permissions.OrgRead)]
    [ProducesResponseType(typeof(OrganizationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizationDetailDto>> GetOrganization(
        Guid organizationId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetOrganizationDetailQuery(organizationId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    public class CreateOrganizationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? ContactEmail { get; set; }
        public string? LogoUrl { get; set; }
    }

    [HttpPost("organizations")]
    [RequirePermission(Permissions.OrgCreate)]
    [ProducesResponseType(typeof(OrganizationDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizationDetailDto>> CreateOrganization(
        [FromBody] CreateOrganizationRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateOrganizationCommand(
                request.Name, request.Slug, request.Description,
                request.ContactEmail, request.LogoUrl), ct);
            return CreatedAtAction(nameof(GetOrganization), new { organizationId = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    public class UpdateOrganizationRequest : CreateOrganizationRequest
    {
        public bool IsActive { get; set; } = true;
    }

    [HttpPut("organizations/{organizationId:guid}")]
    [RequirePermission(Permissions.OrgUpdate)]
    [ProducesResponseType(typeof(OrganizationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizationDetailDto>> UpdateOrganization(
        Guid organizationId,
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateOrganizationCommand(
                organizationId, request.Name, request.Slug, request.Description,
                request.ContactEmail, request.LogoUrl, request.IsActive), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    // ------------------------------------------------------------------
    // Branches (nested under an organization)
    // ------------------------------------------------------------------

    public class CreateBranchRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Location { get; set; }
        public string? ContactEmail { get; set; }
    }

    [HttpPost("organizations/{organizationId:guid}/branches")]
    [RequirePermission(Permissions.BranchCreate)]
    [ProducesResponseType(typeof(BranchListItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchListItemDto>> CreateBranch(
        Guid organizationId,
        [FromBody] CreateBranchRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateBranchCommand(
                organizationId, request.Name, request.Code, request.Location, request.ContactEmail), ct);
            return CreatedAtAction(nameof(GetOrganization),
                new { organizationId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    public class UpdateBranchRequest : CreateBranchRequest
    {
        public bool IsActive { get; set; } = true;
    }

    [HttpPut("branches/{branchId:guid}")]
    [RequirePermission(Permissions.BranchUpdate)]
    [ProducesResponseType(typeof(BranchListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchListItemDto>> UpdateBranch(
        Guid branchId,
        [FromBody] UpdateBranchRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateBranchCommand(
                branchId, request.Name, request.Code, request.Location,
                request.ContactEmail, request.IsActive), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    // ------------------------------------------------------------------
    // OrgAdmin lifecycle
    // ------------------------------------------------------------------

    public class CreateOrgAdminRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("organizations/{organizationId:guid}/admins")]
    [RequirePermission(Permissions.OrgAdminCreate)]
    [ProducesResponseType(typeof(CreateOrgAdminResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateOrgAdminResult>> CreateOrgAdmin(
        Guid organizationId,
        [FromBody] CreateOrgAdminRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateOrgAdminCommand(
                organizationId, request.Email, request.FirstName,
                request.LastName, request.Password), ct);
            return CreatedAtAction(nameof(GetOrganization),
                new { organizationId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    // ------------------------------------------------------------------
    // Cross-organization transfer (Teachers and Students)
    // ------------------------------------------------------------------

    public class TransferUserRequest
    {
        public Guid OrganizationId { get; set; }
        public Guid BranchId { get; set; }
    }

    [HttpPatch("users/{userId:guid}/transfer")]
    [ProducesResponseType(typeof(Application.Admin.Dtos.AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Application.Admin.Dtos.AdminUserDetailDto>> TransferUser(
        Guid userId,
        [FromBody] TransferUserRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new TransferUserCommand(
                userId, request.OrganizationId, request.BranchId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    // ------------------------------------------------------------------
    // Role-Permission management
    // ------------------------------------------------------------------

    [HttpGet("role-permissions")]
    [ProducesResponseType(typeof(RolePermissionMatrixDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RolePermissionMatrixDto>> GetRolePermissionMatrix(
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRolePermissionMatrixQuery(), ct);
        return Ok(result);
    }

    public class SetRolePermissionsBody
    {
        public string Role { get; set; } = string.Empty;
        public List<string> PermissionCodes { get; set; } = new();
    }

    [HttpPut("role-permissions")]
    [ProducesResponseType(typeof(RolePermissionMatrixDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RolePermissionMatrixDto>> SetRolePermissions(
        [FromBody] SetRolePermissionsBody body, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new SetRolePermissionsCommand(body.Role, body.PermissionCodes), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    // ------------------------------------------------------------------

    private ActionResult ValidationErrors(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return ValidationProblem(new ValidationProblemDetails(errors));
    }
}
