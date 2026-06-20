using System.Text;
using LMS.Application.Attendance.Dtos;
using LMS.Application.Attendance.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Branch-wise attendance reporting for the OrgAdmin. Every query is scoped to the
/// caller's organization inside the handler (OrgAdminScope), so this is a true
/// "all branches in my org" view — and nothing beyond it.
/// </summary>
public partial class OrgAdminController
{
    /// <summary>Org-wide rollup, one row per branch.</summary>
    [HttpGet("attendance/overview")]
    [ProducesResponseType(typeof(OrgAttendanceOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OrgAttendanceOverviewDto>> GetAttendanceOverview(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetOrgAttendanceOverviewQuery(fromDate, toDate), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    /// <summary>Per-course breakdown for one branch (drill-down).</summary>
    [HttpGet("attendance/branches/{branchId:guid}/detail")]
    [ProducesResponseType(typeof(BranchAttendanceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BranchAttendanceDetailDto>> GetBranchAttendanceDetail(
        Guid branchId, [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetBranchAttendanceDetailQuery(branchId, fromDate, toDate), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    /// <summary>Download the branch rollup as CSV.</summary>
    [HttpGet("attendance/overview/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAttendanceOverviewCsv(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        try
        {
            var data = await _mediator.Send(new GetOrgAttendanceOverviewQuery(fromDate, toDate), ct);

            var sb = new StringBuilder();
            sb.AppendLine("Branch,Courses,Sessions,Students,Records,AttendancePercent");
            foreach (var b in data.Branches)
            {
                sb.Append(CsvField(b.BranchName)).Append(',')
                  .Append(b.CourseCount).Append(',')
                  .Append(b.SessionCount).Append(',')
                  .Append(b.StudentCount).Append(',')
                  .Append(b.RecordCount).Append(',')
                  .Append(b.AttendancePercent)
                  .Append('\n');
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"attendance-overview-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    private static string CsvField(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
