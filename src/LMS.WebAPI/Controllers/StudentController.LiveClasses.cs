using System.Linq;
using FluentValidation;
using LMS.Application.LiveClasses.Commands;
using LMS.Application.LiveClasses.Dtos;
using LMS.Application.LiveClasses.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Live classes from the student's side: upcoming/live sessions for enrolled courses,
/// and joining (which auto-marks attendance and returns the video-room details).
/// </summary>
public partial class StudentController
{
    /// <summary>Upcoming + live sessions for the courses the student is enrolled in.</summary>
    [HttpGet("live-sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<LiveSessionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LiveSessionDto>>> GetMyLiveSessions(CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetMyLiveSessionsQuery(), ct));
    }

    /// <summary>Join a live session — marks attendance and returns the room to embed.</summary>
    [HttpPost("live-sessions/{liveSessionId:guid}/join")]
    [ProducesResponseType(typeof(LiveJoinInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LiveJoinInfoDto>> JoinLiveSession(Guid liveSessionId, CancellationToken ct)
    {
        try
        {
            return Ok(await _mediator.Send(new JoinLiveSessionCommand(liveSessionId), ct));
        }
        catch (ValidationException ex) { return BadRequest(new { message = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)) }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
