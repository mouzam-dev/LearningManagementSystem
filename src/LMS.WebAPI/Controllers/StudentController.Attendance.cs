using LMS.Application.Attendance.Dtos;
using LMS.Application.Attendance.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// The calling student's own attendance — overall %, per-course breakdown, and a
/// recent-marks list. Scoped to the authenticated student inside the handler.
/// </summary>
public partial class StudentController
{
    [HttpGet("attendance")]
    [ProducesResponseType(typeof(MyAttendanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MyAttendanceDto>> GetMyAttendance(
        [FromQuery] Guid? courseId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyAttendanceQuery(courseId, fromDate, toDate), ct);
        return Ok(result);
    }
}
