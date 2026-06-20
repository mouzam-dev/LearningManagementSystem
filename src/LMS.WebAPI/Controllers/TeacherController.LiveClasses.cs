using FluentValidation;
using LMS.Application.LiveClasses.Commands;
using LMS.Application.LiveClasses.Dtos;
using LMS.Application.LiveClasses.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Live online class management for the teacher who owns (or co-instructs) the course.
/// Course-ownership is enforced in the handlers; the controller is role-gated to Teacher.
/// </summary>
public partial class TeacherController
{
    public class ScheduleLiveSessionRequest
    {
        public Guid CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ScheduledStart { get; set; }
        public int DurationMinutes { get; set; } = 60;
    }

    public class SetLiveSessionStatusRequest
    {
        public string Status { get; set; } = "Live";
    }

    /// <summary>Schedule a live class for a course.</summary>
    [HttpPost("live-sessions")]
    [ProducesResponseType(typeof(LiveSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LiveSessionDto>> ScheduleLiveSession(
        [FromBody] ScheduleLiveSessionRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ScheduleLiveSessionCommand(
                request.CourseId, request.Title, request.ScheduledStart, request.DurationMinutes), ct);
            return Ok(result);
        }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }

    /// <summary>List a course's live sessions (newest first).</summary>
    [HttpGet("live-sessions/courses/{courseId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<LiveSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LiveSessionDto>>> GetCourseLiveSessions(Guid courseId, CancellationToken ct)
    {
        try
        {
            return Ok(await _mediator.Send(new GetCourseLiveSessionsQuery(courseId), ct));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }

    /// <summary>Start (→ Live), End, or Cancel a live session.</summary>
    [HttpPut("live-sessions/{liveSessionId:guid}/status")]
    [ProducesResponseType(typeof(LiveSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LiveSessionDto>> SetLiveSessionStatus(
        Guid liveSessionId, [FromBody] SetLiveSessionStatusRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new SetLiveSessionStatusCommand(liveSessionId, request.Status), ct);
            return Ok(result);
        }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }
}
