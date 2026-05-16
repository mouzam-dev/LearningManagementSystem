using FluentValidation;
using LMS.Application.OrgAdmin.Commands;
using LMS.Application.OrgAdmin.Dtos;
using LMS.Application.OrgAdmin.Queries;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Org-scoped endpoints reserved for the OrgAdmin role. Every handler reads
/// the caller's <c>org_id</c> JWT claim and refuses to touch resources outside
/// of that organization — there is no &quot;all orgs&quot; view here.
/// </summary>
[ApiController]
[Route("api/orgadmin")]
[Authorize(Roles = Roles.OrgAdmin)]
public class OrgAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrgAdminController(IMediator mediator) { _mediator = mediator; }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(OrgAdminDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrgAdminDashboardDto>> GetDashboard(CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetOrgAdminDashboardQuery(), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ----- Branches ----------------------------------------------------

    [HttpGet("branches")]
    [ProducesResponseType(typeof(IReadOnlyList<BranchListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BranchListItemDto>>> GetBranches(CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetOrgBranchesQuery(), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    public class CreateBranchRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Location { get; set; }
        public string? ContactEmail { get; set; }
    }

    [HttpPost("branches")]
    [ProducesResponseType(typeof(BranchListItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BranchListItemDto>> CreateBranch(
        [FromBody] CreateBranchRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new OrgCreateBranchCommand(
                request.Name, request.Code, request.Location, request.ContactEmail), ct);
            return CreatedAtAction(nameof(GetBranches), null, result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    public class UpdateBranchRequest : CreateBranchRequest
    {
        public bool IsActive { get; set; } = true;
    }

    [HttpPut("branches/{branchId:guid}")]
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
            var result = await _mediator.Send(new OrgUpdateBranchCommand(
                branchId, request.Name, request.Code, request.Location,
                request.ContactEmail, request.IsActive), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    // ----- Teachers ----------------------------------------------------

    [HttpGet("teachers")]
    [ProducesResponseType(typeof(IReadOnlyList<OrgTeacherDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrgTeacherDto>>> GetTeachers(
        [FromQuery] string? search = null,
        [FromQuery] Guid? branchId = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(new GetOrgTeachersQuery(search, branchId), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    public class CreateTeacherRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Guid BranchId { get; set; }
    }

    [HttpPost("teachers")]
    [ProducesResponseType(typeof(OrgTeacherDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrgTeacherDto>> CreateTeacher(
        [FromBody] CreateTeacherRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateOrgTeacherCommand(
                request.FirstName, request.LastName, request.Email,
                request.Password, request.BranchId), ct);
            return CreatedAtAction(nameof(GetTeachers), null, result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    public class AssignBranchRequest
    {
        public Guid BranchId { get; set; }
    }

    [HttpPatch("teachers/{teacherId:guid}/branch")]
    [ProducesResponseType(typeof(OrgTeacherDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrgTeacherDto>> AssignTeacherBranch(
        Guid teacherId,
        [FromBody] AssignBranchRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new AssignTeacherBranchCommand(teacherId, request.BranchId), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationErrors(ex); }
    }

    // ----- Course moderation -------------------------------------------

    [HttpGet("courses")]
    [ProducesResponseType(typeof(OrgAdminCoursesPage), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrgAdminCoursesPage>> GetCourses(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(new GetOrgCoursesQuery(
                search, category, isPublished, branchId, page, pageSize), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [HttpGet("courses/{courseId:guid}")]
    [ProducesResponseType(typeof(OrgAdminCourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrgAdminCourseDetailDto>> GetCourseDetail(
        Guid courseId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetOrgCourseDetailQuery(courseId), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    public class SetCoursePublishedRequest
    {
        public bool IsPublished { get; set; }
    }

    [HttpPatch("courses/{courseId:guid}/published")]
    [ProducesResponseType(typeof(OrgAdminCourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrgAdminCourseDetailDto>> SetCoursePublished(
        Guid courseId,
        [FromBody] SetCoursePublishedRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new OrgSetCoursePublishedCommand(courseId, request.IsPublished), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("courses/{courseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(Guid courseId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new OrgDeleteCourseCommand(courseId), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
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
