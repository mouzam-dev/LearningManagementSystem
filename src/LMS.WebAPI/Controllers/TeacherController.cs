using FluentValidation;
using LMS.Application.Teacher.Commands;
using LMS.Application.Teacher.Dtos;
using LMS.Application.Teacher.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

[ApiController]
[Route("api/teacher")]
[Authorize(Roles = "Teacher")]
public class TeacherController : ControllerBase
{
    private readonly IMediator _mediator;

    public TeacherController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(TeacherDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeacherDashboardDto>> GetDashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTeacherDashboardQuery(), ct);
        return Ok(result);
    }

    [HttpGet("courses")]
    [ProducesResponseType(typeof(IReadOnlyList<TeacherCourseListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeacherCourseListItemDto>>> GetCourses(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTeacherCoursesQuery(), ct);
        return Ok(result);
    }

    public class CreateCourseRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int? MaxStudents { get; set; }
    }

    [HttpPost("courses")]
    [ProducesResponseType(typeof(CreatedCourseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatedCourseDto>> CreateCourse(
        [FromBody] CreateCourseRequest request,
        CancellationToken ct)
    {
        try
        {
            var command = new CreateCourseCommand(
                request.Title,
                request.Description,
                request.Category,
                request.ThumbnailUrl,
                request.MaxStudents);

            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(
                nameof(GetCourses),
                new { },
                result);
        }
        catch (ValidationException ex)
        {
            // Surface field-level errors so the Angular form can highlight the
            // offending control. Keys are property names from the command;
            // values are arrays of human-readable messages.
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }
    }
}
