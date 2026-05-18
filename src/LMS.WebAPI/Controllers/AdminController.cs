using System.Text;
using FluentValidation;
using LMS.Application.Admin.Commands;
using LMS.Application.Admin.Dtos;
using LMS.Application.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAdminDashboardQuery(), ct);
        return Ok(result);
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(AdminUsersPage), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminUsersPage>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAdminUsersQuery(search, role, isActive, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDetailDto>> GetUserDetail(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetAdminUserDetailQuery(userId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    public class SetActiveRequest
    {
        public bool IsActive { get; set; }
    }

    [HttpPatch("users/{userId:guid}/active")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserDetailDto>> SetActive(
        Guid userId,
        [FromBody] SetActiveRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new SetUserActiveCommand(userId, request.IsActive), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    public class ChangeRoleRequest
    {
        public string Role { get; set; } = string.Empty;
        /// <summary>Required when promoting to OrgAdmin.</summary>
        public Guid? OrganizationId { get; set; }
        /// <summary>Required when assigning Teacher or Student.</summary>
        public Guid? BranchId { get; set; }
    }

    [HttpPatch("users/{userId:guid}/role")]
    [ProducesResponseType(typeof(AdminUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserDetailDto>> ChangeRole(
        Guid userId,
        [FromBody] ChangeRoleRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ChangeUserRoleCommand(
                userId, request.Role, request.OrganizationId, request.BranchId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }
    }

    // -----------------------------------------------------------------------
    // Course moderation
    // -----------------------------------------------------------------------

    [HttpGet("courses")]
    [ProducesResponseType(typeof(AdminCoursesPage), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminCoursesPage>> GetCourses(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAdminCoursesQuery(search, category, isPublished, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("courses/{courseId:guid}")]
    [ProducesResponseType(typeof(AdminCourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCourseDetailDto>> GetCourseDetail(Guid courseId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetAdminCourseDetailQuery(courseId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    public class SetCoursePublishedRequest
    {
        public bool IsPublished { get; set; }
    }

    [HttpPatch("courses/{courseId:guid}/published")]
    [ProducesResponseType(typeof(AdminCourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCourseDetailDto>> SetCoursePublished(
        Guid courseId,
        [FromBody] SetCoursePublishedRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new SetCourseModeratedCommand(courseId, request.IsPublished), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("courses/{courseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(Guid courseId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteCourseCommand(courseId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // -----------------------------------------------------------------------
    // Audit log
    // -----------------------------------------------------------------------

    [HttpGet("audit")]
    [ProducesResponseType(typeof(AdminAuditLogPage), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminAuditLogPage>> GetAuditLog(
        [FromQuery] string? entity = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAuditLogQuery(entity, page, pageSize), ct);
        return Ok(result);
    }

    // -----------------------------------------------------------------------
    // Reporting + CSV exports
    // -----------------------------------------------------------------------

    [HttpGet("reports")]
    [ProducesResponseType(typeof(AdminReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminReportDto>> GetReport(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAdminReportQuery(), ct);
        return Ok(result);
    }

    [HttpGet("reports/users.csv")]
    public async Task<IActionResult> ExportUsersCsv(CancellationToken ct)
    {
        var rows = await _mediator.Send(new GetUsersExportQuery(), ct);

        var sb = new StringBuilder();
        sb.AppendLine("First name,Last name,Email,Role,Active,Verified,Joined");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',',
                Csv(r.FirstName), Csv(r.LastName), Csv(r.Email), Csv(r.Role),
                r.IsActive ? "Yes" : "No", r.IsVerified ? "Yes" : "No",
                r.CreatedAt.ToString("yyyy-MM-dd")));
        }

        return CsvFile(sb, "lms-users");
    }

    [HttpGet("reports/courses.csv")]
    public async Task<IActionResult> ExportCoursesCsv(CancellationToken ct)
    {
        var rows = await _mediator.Send(new GetCoursesExportQuery(), ct);

        var sb = new StringBuilder();
        sb.AppendLine("Title,Category,Teacher,Teacher email,Published,Modules,Lessons,Students,Created");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(',',
                Csv(r.Title), Csv(r.Category), Csv(r.TeacherName), Csv(r.TeacherEmail),
                r.IsPublished ? "Yes" : "No",
                r.ModuleCount, r.LessonCount, r.StudentCount,
                r.CreatedAt.ToString("yyyy-MM-dd")));
        }

        return CsvFile(sb, "lms-courses");
    }

    /// <summary>RFC-4180 field escaping: quote when the value holds a comma, quote, or newline.</summary>
    private static string Csv(string? value)
    {
        var v = value ?? string.Empty;
        if (v.Contains(',') || v.Contains('"') || v.Contains('\n') || v.Contains('\r'))
        {
            return $"\"{v.Replace("\"", "\"\"")}\"";
        }
        return v;
    }

    private FileContentResult CsvFile(StringBuilder sb, string baseName)
    {
        var fileName = $"{baseName}-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        // UTF-8 BOM so Excel opens accented characters correctly.
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
        return File(bytes, "text/csv", fileName);
    }

    // -----------------------------------------------------------------------
    // Bulk user import (BRD ADM-012). Multipart CSV upload; each row creates
    // a user with a random password + queues a password-reset email so the
    // user picks their own credentials on first sign-in.
    // -----------------------------------------------------------------------

    [HttpPost("users/bulk-import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2 * 1024 * 1024)] // 2MB cap — covers ~1000 rows easily
    [ProducesResponseType(typeof(BulkImportUsersResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkImportUsersResultDto>> BulkImportUsers(
        IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Upload a non-empty CSV file." });
        }

        List<BulkImportUserRow> rows;
        try
        {
            rows = await ParseCsvAsync(file, ct);
        }
        catch (FormatException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (rows.Count == 0)
        {
            return BadRequest(new { message = "CSV had no data rows after the header." });
        }

        try
        {
            var result = await _mediator.Send(new BulkImportUsersCommand(rows), ct);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid request." });
        }
    }

    /// <summary>
    /// Minimal CSV parser tailored to the bulk-import format. Handles the
    /// common cases: comma-separated fields, optional double-quoted fields,
    /// and "" inside quoted fields as an escaped quote. Refuses to do
    /// anything clever — admins should regenerate via the template if they
    /// hit a weird edge case.
    /// </summary>
    private static async Task<List<BulkImportUserRow>> ParseCsvAsync(IFormFile file, CancellationToken ct)
    {
        // Expected header order — we map by header name (case-insensitive) so
        // admins can reorder columns without breaking the import.
        var expected = new[] { "Email", "FirstName", "LastName", "Role", "OrganizationSlug", "BranchName" };

        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new FormatException("CSV is empty.");
        }

        var header = SplitCsvLine(headerLine).Select(h => h.Trim()).ToList();
        var idx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++) idx[header[i]] = i;

        foreach (var col in new[] { "Email", "FirstName", "LastName", "Role" })
        {
            if (!idx.ContainsKey(col))
            {
                throw new FormatException(
                    $"CSV header missing required column '{col}'. Expected: {string.Join(",", expected)}");
            }
        }

        var rows = new List<BulkImportUserRow>();
        var lineNumber = 1;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue; // skip blanks

            var cells = SplitCsvLine(line);
            string Get(string col)
            {
                if (!idx.TryGetValue(col, out var i) || i >= cells.Count) return string.Empty;
                return cells[i];
            }

            rows.Add(new BulkImportUserRow
            {
                Line = lineNumber,
                Email = Get("Email"),
                FirstName = Get("FirstName"),
                LastName = Get("LastName"),
                Role = Get("Role"),
                OrganizationSlug = Get("OrganizationSlug"),
                BranchName = Get("BranchName"),
            });
        }

        return rows;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    // Doubled "" -> literal quote, otherwise end of quoted field.
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                if (c == ',') { result.Add(sb.ToString()); sb.Clear(); }
                else if (c == '"' && sb.Length == 0) inQuotes = true;
                else sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }
}
