using FluentValidation;
using LMS.Application.Announcements.Commands;
using LMS.Application.Announcements.Dtos;
using LMS.Application.Announcements.Queries;
using LMS.Application.Common;
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
    private readonly IFileStorage _fileStorage;

    public TeacherController(IMediator mediator, IFileStorage fileStorage)
    {
        _mediator = mediator;
        _fileStorage = fileStorage;
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
            return CreatedAtAction(nameof(GetCourses), new { }, result);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    // -----------------------------------------------------------------------
    // Course detail / edit / publish / delete
    // -----------------------------------------------------------------------

    [HttpGet("courses/{courseId:guid}")]
    [ProducesResponseType(typeof(TeacherCourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherCourseDetailDto>> GetCourseDetail(Guid courseId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetTeacherCourseDetailQuery(courseId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    public class UpdateCourseRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int? MaxStudents { get; set; }
    }

    [HttpPut("courses/{courseId:guid}")]
    [ProducesResponseType(typeof(TeacherCourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherCourseDetailDto>> UpdateCourse(
        Guid courseId,
        [FromBody] UpdateCourseRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateCourseCommand(
                courseId, request.Title, request.Description, request.Category,
                request.ThumbnailUrl, request.MaxStudents), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    public class SetPublishedRequest
    {
        public bool IsPublished { get; set; }
    }

    [HttpPatch("courses/{courseId:guid}/published")]
    [ProducesResponseType(typeof(TeacherCourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeacherCourseDetailDto>> SetPublished(
        Guid courseId,
        [FromBody] SetPublishedRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new SetCoursePublishedCommand(courseId, request.IsPublished), ct);
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

    // -----------------------------------------------------------------------
    // Modules
    // -----------------------------------------------------------------------

    public class CreateModuleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    [HttpPost("courses/{courseId:guid}/modules")]
    [ProducesResponseType(typeof(TeacherModuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherModuleDto>> CreateModule(
        Guid courseId,
        [FromBody] CreateModuleRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateModuleCommand(courseId, request.Title, request.Description), ct);
            return Created(string.Empty, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    public class UpdateModuleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Order { get; set; }
    }

    [HttpPut("modules/{moduleId:guid}")]
    [ProducesResponseType(typeof(TeacherModuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherModuleDto>> UpdateModule(
        Guid moduleId,
        [FromBody] UpdateModuleRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateModuleCommand(moduleId, request.Title, request.Description, request.Order), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    [HttpDelete("modules/{moduleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteModule(Guid moduleId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteModuleCommand(moduleId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // -----------------------------------------------------------------------
    // Lessons
    // -----------------------------------------------------------------------

    public class CreateLessonRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "Video";
        public string? Content { get; set; }
        public int? Duration { get; set; }
        public bool IsPublished { get; set; } = true;
    }

    [HttpPost("modules/{moduleId:guid}/lessons")]
    [ProducesResponseType(typeof(TeacherLessonDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherLessonDto>> CreateLesson(
        Guid moduleId,
        [FromBody] CreateLessonRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateLessonCommand(
                moduleId, request.Title, request.Type, request.Content,
                request.Duration, request.IsPublished), ct);
            return Created(string.Empty, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    public class UpdateLessonRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "Video";
        public string? Content { get; set; }
        public int? Duration { get; set; }
        public int? Order { get; set; }
        public bool IsPublished { get; set; } = true;
    }

    [HttpPut("lessons/{lessonId:guid}")]
    [ProducesResponseType(typeof(TeacherLessonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherLessonDto>> UpdateLesson(
        Guid lessonId,
        [FromBody] UpdateLessonRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateLessonCommand(
                lessonId, request.Title, request.Type, request.Content,
                request.Duration, request.Order, request.IsPublished), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    [HttpDelete("lessons/{lessonId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLesson(Guid lessonId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteLessonCommand(lessonId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // -----------------------------------------------------------------------
    // Assessments
    // -----------------------------------------------------------------------

    [HttpGet("assessments/{assessmentId:guid}")]
    [ProducesResponseType(typeof(TeacherAssessmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherAssessmentDto>> GetAssessment(Guid assessmentId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetTeacherAssessmentQuery(assessmentId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    public class CreateAssessmentRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "Quiz";
        public int? TimeLimit { get; set; }
        public int PassingScore { get; set; } = 70;
        public DateTime? DueDate { get; set; }
        public int? MaxAttempts { get; set; }
    }

    [HttpPost("courses/{courseId:guid}/assessments")]
    [ProducesResponseType(typeof(TeacherAssessmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherAssessmentDto>> CreateAssessment(
        Guid courseId,
        [FromBody] CreateAssessmentRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateAssessmentCommand(
                courseId, request.Title, request.Type, request.TimeLimit,
                request.PassingScore, request.DueDate, request.MaxAttempts), ct);
            return Created(string.Empty, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    public class UpdateAssessmentRequest
    {
        public string Title { get; set; } = string.Empty;
        public int? TimeLimit { get; set; }
        public int PassingScore { get; set; }
        public DateTime? DueDate { get; set; }
        public int? MaxAttempts { get; set; }
    }

    [HttpPut("assessments/{assessmentId:guid}")]
    [ProducesResponseType(typeof(TeacherAssessmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherAssessmentDto>> UpdateAssessment(
        Guid assessmentId,
        [FromBody] UpdateAssessmentRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateAssessmentCommand(
                assessmentId, request.Title, request.TimeLimit, request.PassingScore,
                request.DueDate, request.MaxAttempts), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    [HttpDelete("assessments/{assessmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAssessment(Guid assessmentId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteAssessmentCommand(assessmentId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // -----------------------------------------------------------------------
    // Questions
    // -----------------------------------------------------------------------

    public class CreateQuestionRequest
    {
        public string QuestionText { get; set; } = string.Empty;
        public string Type { get; set; } = "MCQ";
        public List<string>? Options { get; set; }
        public string? CorrectAnswer { get; set; }
        public int Points { get; set; } = 1;
    }

    [HttpPost("assessments/{assessmentId:guid}/questions")]
    [ProducesResponseType(typeof(TeacherQuestionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherQuestionDto>> CreateQuestion(
        Guid assessmentId,
        [FromBody] CreateQuestionRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateQuestionCommand(
                assessmentId, request.QuestionText, request.Type,
                request.Options, request.CorrectAnswer, request.Points), ct);
            return Created(string.Empty, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    public class UpdateQuestionRequest
    {
        public string QuestionText { get; set; } = string.Empty;
        public string Type { get; set; } = "MCQ";
        public List<string>? Options { get; set; }
        public string? CorrectAnswer { get; set; }
        public int Points { get; set; } = 1;
        public int? Order { get; set; }
    }

    [HttpPut("questions/{questionId:guid}")]
    [ProducesResponseType(typeof(TeacherQuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherQuestionDto>> UpdateQuestion(
        Guid questionId,
        [FromBody] UpdateQuestionRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateQuestionCommand(
                questionId, request.QuestionText, request.Type,
                request.Options, request.CorrectAnswer, request.Points, request.Order), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    [HttpDelete("questions/{questionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuestion(Guid questionId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteQuestionCommand(questionId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // -----------------------------------------------------------------------
    // Grading
    // -----------------------------------------------------------------------

    [HttpGet("grading/queue")]
    [ProducesResponseType(typeof(IReadOnlyList<GradingQueueItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GradingQueueItemDto>>> GetGradingQueue(
        [FromQuery] bool includeGraded = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetGradingQueueQuery(includeGraded), ct);
        return Ok(result);
    }

    [HttpGet("submissions/{submissionId:guid}")]
    [ProducesResponseType(typeof(TeacherSubmissionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherSubmissionDetailDto>> GetSubmission(
        Guid submissionId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetTeacherSubmissionQuery(submissionId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    public class GradeSubmissionRequest
    {
        public int Score { get; set; }
        public string? Feedback { get; set; }
    }

    [HttpPut("submissions/{submissionId:guid}/grade")]
    [ProducesResponseType(typeof(TeacherSubmissionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeacherSubmissionDetailDto>> GradeSubmission(
        Guid submissionId,
        [FromBody] GradeSubmissionRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new GradeSubmissionCommand(submissionId, request.Score, request.Feedback), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(BuildModelState(ex));
        }
    }

    // -----------------------------------------------------------------------
    // Per-course students + analytics
    // -----------------------------------------------------------------------

    [HttpGet("courses/{courseId:guid}/students")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseStudentListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CourseStudentListItemDto>>> GetCourseStudents(
        Guid courseId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCourseStudentsQuery(courseId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("courses/{courseId:guid}/students/{studentId:guid}")]
    [ProducesResponseType(typeof(CourseStudentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseStudentDetailDto>> GetCourseStudentDetail(
        Guid courseId,
        Guid studentId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCourseStudentDetailQuery(courseId, studentId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("courses/{courseId:guid}/analytics")]
    [ProducesResponseType(typeof(CourseAnalyticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseAnalyticsDto>> GetCourseAnalytics(
        Guid courseId,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCourseAnalyticsQuery(courseId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // -----------------------------------------------------------------------
    // Document upload (for Document-type lessons)
    // -----------------------------------------------------------------------

    public class DocumentUploadResult
    {
        /// <summary>Absolute URL the saved file is served from.</summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>The original filename, shown to students in the player.</summary>
        public string FileName { get; set; } = string.Empty;
    }

    private static readonly HashSet<string> AllowedDocExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".txt",
    };
    private const long MaxDocBytes = 20 * 1024 * 1024; // 20 MB

    [HttpPost("uploads/document")]
    [ProducesResponseType(typeof(DocumentUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(MaxDocBytes + 1024 * 1024)] // a little headroom over the app check
    public async Task<ActionResult<DocumentUploadResult>> UploadDocument(
        [FromForm] IFormFile? file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["file"] = ["Choose a file to upload."] }));
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedDocExtensions.Contains(ext))
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["file"] = [$"Unsupported file type. Allowed: {string.Join(", ", AllowedDocExtensions)}"],
                }));
        }

        if (file.Length > MaxDocBytes)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]>
                {
                    ["file"] = ["File is too large — the limit is 20 MB."],
                }));
        }

        await using var stream = file.OpenReadStream();
        var relativePath = await _fileStorage.SaveAsync(stream, "uploads/documents", ext, ct);

        // Hand back an absolute URL so the stored lesson Content works from any origin.
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";
        return Ok(new DocumentUploadResult
        {
            Url = absoluteUrl,
            FileName = Path.GetFileName(file.FileName),
        });
    }

    // -----------------------------------------------------------------------
    // Helper: turn a FluentValidation ValidationException into a
    // ValidationProblemDetails-friendly dictionary keyed by property name.
    // -----------------------------------------------------------------------

    private static ValidationProblemDetails BuildModelState(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return new ValidationProblemDetails(errors);
    }

    // -----------------------------------------------------------------------
    // Announcements (per course owned by this teacher). Posting fans out to
    // every enrolled student as an in-app notification.
    // -----------------------------------------------------------------------

    [HttpGet("courses/{courseId:guid}/announcements")]
    [ProducesResponseType(typeof(IReadOnlyList<AnnouncementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AnnouncementDto>>> GetCourseAnnouncements(
        Guid courseId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCourseAnnouncementsQuery(courseId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    public class CreateAnnouncementRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
    }

    [HttpPost("courses/{courseId:guid}/announcements")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnouncementDto>> CreateAnnouncement(
        Guid courseId,
        [FromBody] CreateAnnouncementRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateAnnouncementCommand(
                courseId, request.Title, request.Body, request.IsPinned), ct);
            return CreatedAtAction(nameof(GetCourseAnnouncements), new { courseId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
    }
}
