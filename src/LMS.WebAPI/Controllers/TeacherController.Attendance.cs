using FluentValidation;
using LMS.Application.Attendance.Commands;
using LMS.Application.Attendance.Dtos;
using LMS.Application.Attendance.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Attendance endpoints for the teacher who owns (or co-instructs) the course.
/// Course-ownership is enforced inside the handlers; the controller is already
/// role-gated to Teacher.
/// </summary>
public partial class TeacherController
{
    public class CreateAttendanceSessionRequest
    {
        public Guid CourseId { get; set; }
        public DateOnly SessionDate { get; set; }
        public int Slot { get; set; } = 1;
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Topic { get; set; }
    }

    public class MarkAttendanceRequest
    {
        public List<MarkInputDto> Marks { get; set; } = new();
    }

    public class SetAttendanceSessionStatusRequest
    {
        public string Status { get; set; } = "Finalized";
    }

    /// <summary>Open a new session for a date/slot and snapshot the roster.</summary>
    [HttpPost("attendance/sessions")]
    [ProducesResponseType(typeof(SessionRosterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SessionRosterDto>> CreateAttendanceSession(
        [FromBody] CreateAttendanceSessionRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateAttendanceSessionCommand(
                request.CourseId, request.SessionDate, request.Slot,
                request.StartTime, request.EndTime, request.Topic), ct);
            return Ok(result);
        }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>List a course's sessions (newest first) with per-session counts.</summary>
    [HttpGet("attendance/courses/{courseId:guid}/sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<AttendanceSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AttendanceSessionDto>>> GetCourseAttendanceSessions(
        Guid courseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(new GetCourseAttendanceSessionsQuery(courseId, page, pageSize), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }

    /// <summary>Full roster for one session — the marking grid.</summary>
    [HttpGet("attendance/sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(SessionRosterDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SessionRosterDto>> GetAttendanceSessionRoster(Guid sessionId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetAttendanceSessionRosterQuery(sessionId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }

    /// <summary>Bulk-save status changes for an open session.</summary>
    [HttpPost("attendance/sessions/{sessionId:guid}/marks")]
    [ProducesResponseType(typeof(SessionRosterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SessionRosterDto>> MarkAttendance(
        Guid sessionId, [FromBody] MarkAttendanceRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new MarkAttendanceCommand(sessionId, request.Marks ?? new()), ct);
            return Ok(result);
        }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Finalize (lock), re-open, or cancel a session.</summary>
    [HttpPut("attendance/sessions/{sessionId:guid}/status")]
    [ProducesResponseType(typeof(AttendanceSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttendanceSessionDto>> SetAttendanceSessionStatus(
        Guid sessionId, [FromBody] SetAttendanceSessionStatusRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new SetAttendanceSessionStatusCommand(sessionId, request.Status), ct);
            return Ok(result);
        }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }

    /// <summary>Per-student attendance % across the whole course.</summary>
    [HttpGet("attendance/courses/{courseId:guid}/summary")]
    [ProducesResponseType(typeof(CourseAttendanceSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CourseAttendanceSummaryDto>> GetCourseAttendanceSummary(Guid courseId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCourseAttendanceSummaryQuery(courseId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
    }
}
