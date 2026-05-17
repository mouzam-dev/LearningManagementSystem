using LMS.Application.Announcements.Dtos;
using LMS.Application.Announcements.Queries;
using LMS.Application.Common;
using LMS.Application.Student.Commands;
using LMS.Application.Student.Dtos;
using LMS.Application.Student.Queries;
using LMS.Application.Student.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

[ApiController]
[Route("api/student")]
[Authorize(Roles = "Student")]
public class StudentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICertificatePdfService _certificatePdfService;
    private readonly IConfiguration _configuration;

    public StudentController(
        IMediator mediator,
        ICertificatePdfService certificatePdfService,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _certificatePdfService = certificatePdfService;
        _configuration = configuration;
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> GetDashboard(CancellationToken ct)
    {
        var dashboard = await _mediator.Send(new GetDashboardQuery(), ct);
        return Ok(dashboard);
    }

    [HttpGet("courses")]
    [ProducesResponseType(typeof(PagedResult<CourseListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CourseListItemDto>>> GetCourses(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetCoursesQuery(search, category, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken ct)
    {
        var categories = await _mediator.Send(new GetCategoriesQuery(), ct);
        return Ok(categories);
    }

    [HttpPost("enroll/{courseId:guid}")]
    [ProducesResponseType(typeof(CourseListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CourseListItemDto>> Enroll(Guid courseId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new EnrollCommand(courseId), ct);
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

    [HttpGet("courses/{courseId:guid}")]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CourseDetailDto>> GetCourseDetail(Guid courseId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCourseDetailQuery(courseId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(LessonDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LessonDetailDto>> GetLesson(Guid lessonId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetLessonQuery(lessonId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    public class UpdateLessonProgressRequest
    {
        public int WatchTimeSeconds { get; set; }
        public bool MarkComplete { get; set; }
    }

    public class SubmitAssessmentRequest
    {
        /// <summary>Map of QuestionId (string Guid) → student's answer.</summary>
        public Dictionary<string, string> Answers { get; set; } = new();
    }

    [HttpPut("lessons/{lessonId:guid}/progress")]
    [ProducesResponseType(typeof(LessonDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LessonDetailDto>> UpdateLessonProgress(
        Guid lessonId,
        [FromBody] UpdateLessonProgressRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateLessonProgressCommand(lessonId, request.WatchTimeSeconds, request.MarkComplete), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("courses/{courseId:guid}/assessments")]
    [ProducesResponseType(typeof(IReadOnlyList<AssessmentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AssessmentListItemDto>>> GetCourseAssessments(
        Guid courseId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCourseAssessmentsQuery(courseId), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("assessments/{assessmentId:guid}")]
    [ProducesResponseType(typeof(AssessmentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AssessmentDetailDto>> GetAssessment(Guid assessmentId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetAssessmentQuery(assessmentId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("certificates")]
    [ProducesResponseType(typeof(IReadOnlyList<CertificateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CertificateSummaryDto>>> GetCertificates(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyCertificatesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("certificates/{certificateId:guid}")]
    [ProducesResponseType(typeof(CertificateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CertificateDto>> GetCertificate(Guid certificateId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCertificateQuery(certificateId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Download the certificate as a PDF. Re-uses <see cref="GetCertificateQuery"/>
    /// (which enforces the ownership check) and pipes the DTO through QuestPDF.
    /// Public verify URL is embedded so anyone holding the PDF can confirm it.
    /// </summary>
    [HttpGet("certificates/{certificateId:guid}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCertificatePdf(Guid certificateId, CancellationToken ct)
    {
        CertificateDto cert;
        try
        {
            cert = await _mediator.Send(new GetCertificateQuery(certificateId), ct);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }

        // Prefer App:PublicWebUrl (set in App Service config for prod) so the
        // embedded link points at the public hostname even when the API is
        // behind a proxy. Fall back to the request origin in dev / local runs.
        var publicWebUrl = _configuration["App:PublicWebUrl"];
        var origin = !string.IsNullOrWhiteSpace(publicWebUrl)
            ? publicWebUrl.TrimEnd('/')
            : $"{Request.Scheme}://{Request.Host}";
        var verifyUrl = $"{origin}/verify/{Uri.EscapeDataString(cert.VerifyCode)}";

        var pdf = _certificatePdfService.Render(cert, verifyUrl);

        var fileName = $"Certificate-{cert.VerifyCode}.pdf";
        return File(pdf, "application/pdf", fileName);
    }

    [HttpPost("assessments/{assessmentId:guid}/submit")]
    [ProducesResponseType(typeof(SubmissionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmissionResultDto>> SubmitAssessment(
        Guid assessmentId,
        [FromBody] SubmitAssessmentRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new SubmitAssessmentCommand(assessmentId, request.Answers), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Recent announcements from courses the student is enrolled in. Powers
    /// the dashboard announcements widget (BRD STU-013 + STU-054).
    /// </summary>
    [HttpGet("announcements")]
    [ProducesResponseType(typeof(IReadOnlyList<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetMyAnnouncements(
        [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMyAnnouncementsQuery(limit), ct);
        return Ok(result);
    }
}
