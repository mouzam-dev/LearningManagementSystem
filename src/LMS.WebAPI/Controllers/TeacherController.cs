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
public partial class TeacherController : ControllerBase
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
    public async Task<ActionResult<IReadOnlyList<TeacherCourseListItemDto>>> GetCourses(
        [FromQuery] bool includeArchived = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTeacherCoursesQuery(includeArchived), ct);
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
        public bool IsFinalExam { get; set; }
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
                request.PassingScore, request.DueDate, request.MaxAttempts, request.IsFinalExam), ct);
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
        /// <summary>Optional rubric (TCH-025); ignored for Quiz-type assessments.</summary>
        public Guid? RubricId { get; set; }
        public bool IsFinalExam { get; set; }
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
                request.DueDate, request.MaxAttempts, request.RubricId, request.IsFinalExam), ct);
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
        /// <summary>Direct score 0-100. Required unless CriterionScores is provided.</summary>
        public int? Score { get; set; }
        public string? Feedback { get; set; }
        /// <summary>
        /// Rubric breakdown (TCH-025) — map of criterionId (string Guid) to points.
        /// When present, the server sums + clamps to 0-100, persists the breakdown,
        /// and uses the sum as the submission Score.
        /// </summary>
        public Dictionary<string, int>? CriterionScores { get; set; }
    }

    [HttpPut("submissions/{submissionId:guid}/grade")]
    [ProducesResponseType(typeof(TeacherSubmissionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeacherSubmissionDetailDto>> GradeSubmission(
        Guid submissionId,
        [FromBody] GradeSubmissionRequest request,
        CancellationToken ct)
    {
        try
        {
            // Parse string-keyed criterion map into Guid keys (the handler validates ownership).
            IReadOnlyDictionary<Guid, int>? cs = null;
            if (request.CriterionScores is { Count: > 0 } raw)
            {
                cs = raw
                    .Where(kv => Guid.TryParse(kv.Key, out _))
                    .ToDictionary(kv => Guid.Parse(kv.Key), kv => kv.Value);
            }

            var result = await _mediator.Send(
                new GradeSubmissionCommand(submissionId, request.Score, request.Feedback, cs), ct);
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
    // Co-instructors (TCH-007)
    // -----------------------------------------------------------------------

    [HttpGet("courses/{courseId:guid}/instructors")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseInstructorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CourseInstructorDto>>> GetCourseInstructors(
        Guid courseId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetCourseInstructorsQuery(courseId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("courses/{courseId:guid}/instructors/eligible")]
    [ProducesResponseType(typeof(IReadOnlyList<EligibleCoInstructorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EligibleCoInstructorDto>>> GetEligibleCoInstructors(
        Guid courseId,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(new GetEligibleCoInstructorsQuery(courseId, search), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    public class AddCoInstructorRequest
    {
        public Guid UserId { get; set; }
    }

    [HttpPost("courses/{courseId:guid}/instructors")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseInstructorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<CourseInstructorDto>>> AddCoInstructor(
        Guid courseId, [FromBody] AddCoInstructorRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new AddCourseCoInstructorCommand(courseId, request.UserId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
    }

    [HttpDelete("courses/{courseId:guid}/instructors/{userId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<CourseInstructorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyList<CourseInstructorDto>>> RemoveCoInstructor(
        Guid courseId, Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new RemoveCourseCoInstructorCommand(courseId, userId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // -----------------------------------------------------------------------
    // Archive + Duplicate (TCH-005 / TCH-006)
    // -----------------------------------------------------------------------

    public class SetArchivedRequest
    {
        public bool IsArchived { get; set; }
    }

    [HttpPatch("courses/{courseId:guid}/archived")]
    [ProducesResponseType(typeof(TeacherCourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherCourseDetailDto>> SetCourseArchived(
        Guid courseId, [FromBody] SetArchivedRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new SetCourseArchivedCommand(courseId, request.IsArchived), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    public class DuplicateCourseRequest
    {
        public string? NewTitle { get; set; }
    }

    [HttpPost("courses/{courseId:guid}/duplicate")]
    [ProducesResponseType(typeof(CreatedCourseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatedCourseDto>> DuplicateCourse(
        Guid courseId, [FromBody] DuplicateCourseRequest? request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new DuplicateCourseCommand(courseId, request?.NewTitle), ct);
            return CreatedAtAction(nameof(GetCourses), null, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
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
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentUploadResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(MaxDocBytes + 1024 * 1024)]
    public async Task<ActionResult<DocumentUploadResult>> UploadDocument(
        IFormFile? file,
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

    // -----------------------------------------------------------------------
    // Question bank (BRD TCH-021) — private per-teacher reusable question pool.
    // Importing into an assessment COPIES rows so future bank edits don't
    // silently alter past assessments.
    // -----------------------------------------------------------------------

    [HttpGet("question-bank")]
    [ProducesResponseType(typeof(PagedResult<BankQuestionListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<BankQuestionListItemDto>>> GetBankQuestions(
        [FromQuery] string? search,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBankQuestionsQuery(search, tag, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("question-bank/tags")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBankQuestionTags(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBankQuestionTagsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("question-bank/{id:guid}")]
    [ProducesResponseType(typeof(BankQuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankQuestionDto>> GetBankQuestion(Guid id, CancellationToken ct)
    {
        try { return Ok(await _mediator.Send(new GetBankQuestionQuery(id), ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    public class BankQuestionUpsertRequest
    {
        public string QuestionText { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<string>? Options { get; set; }
        public string? CorrectAnswer { get; set; }
        public int Points { get; set; } = 1;
        public string? Tag { get; set; }
    }

    [HttpPost("question-bank")]
    [ProducesResponseType(typeof(BankQuestionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BankQuestionDto>> CreateBankQuestion(
        [FromBody] BankQuestionUpsertRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateBankQuestionCommand(
                request.QuestionText, request.Type, request.Options,
                request.CorrectAnswer, request.Points, request.Tag), ct);
            return CreatedAtAction(nameof(GetBankQuestion), new { id = result.Id }, result);
        }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
    }

    [HttpPut("question-bank/{id:guid}")]
    [ProducesResponseType(typeof(BankQuestionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankQuestionDto>> UpdateBankQuestion(
        Guid id, [FromBody] BankQuestionUpsertRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateBankQuestionCommand(
                id, request.QuestionText, request.Type, request.Options,
                request.CorrectAnswer, request.Points, request.Tag), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
    }

    [HttpDelete("question-bank/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBankQuestion(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteBankQuestionCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    public class ImportBankQuestionsRequest
    {
        public List<Guid> QuestionIds { get; set; } = new();
    }

    [HttpPost("assessments/{assessmentId:guid}/import-bank-questions")]
    [ProducesResponseType(typeof(TeacherAssessmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeacherAssessmentDto>> ImportBankQuestionsToAssessment(
        Guid assessmentId, [FromBody] ImportBankQuestionsRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ImportBankQuestionsToAssessmentCommand(
                assessmentId, request.QuestionIds), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
    }

    // -----------------------------------------------------------------------
    // Rubrics (BRD TCH-025) — private per-teacher grading templates.
    // -----------------------------------------------------------------------

    [HttpGet("rubrics")]
    [ProducesResponseType(typeof(PagedResult<RubricListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<RubricListItemDto>>> GetRubrics(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRubricsQuery(search, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("rubrics/{id:guid}")]
    [ProducesResponseType(typeof(RubricDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RubricDto>> GetRubric(Guid id, CancellationToken ct)
    {
        try { return Ok(await _mediator.Send(new GetRubricQuery(id), ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    public class RubricUpsertRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<RubricCriterionInput> Criteria { get; set; } = new();
    }

    [HttpPost("rubrics")]
    [ProducesResponseType(typeof(RubricDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RubricDto>> CreateRubric(
        [FromBody] RubricUpsertRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateRubricCommand(
                request.Name, request.Description, request.Criteria), ct);
            return CreatedAtAction(nameof(GetRubric), new { id = result.Id }, result);
        }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
    }

    [HttpPut("rubrics/{id:guid}")]
    [ProducesResponseType(typeof(RubricDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RubricDto>> UpdateRubric(
        Guid id, [FromBody] RubricUpsertRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateRubricCommand(
                id, request.Name, request.Description, request.Criteria), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
    }

    [HttpDelete("rubrics/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRubric(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteRubricCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // -----------------------------------------------------------------------
    // Bulk enroll (BRD TCH-043). CSV upload with a single Email column; each
    // matching active Student is enrolled in the course. Idempotent — already-
    // enrolled rows return Skipped, not Failed.
    // -----------------------------------------------------------------------

    [HttpPost("courses/{courseId:guid}/bulk-enroll")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(1 * 1024 * 1024)] // 1MB cap is plenty for ~50k email lines
    [ProducesResponseType(typeof(BulkEnrollResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkEnrollResultDto>> BulkEnrollStudents(
        Guid courseId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Upload a non-empty CSV file." });
        }

        List<BulkEnrollRow> rows;
        try
        {
            rows = await ParseEnrollCsvAsync(file, ct);
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
            var result = await _mediator.Send(new BulkEnrollStudentsCommand(courseId, rows), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid request." });
        }
    }

    private static async Task<List<BulkEnrollRow>> ParseEnrollCsvAsync(IFormFile file, CancellationToken ct)
    {
        using var stream = file.OpenReadStream();
        using var reader = new System.IO.StreamReader(stream);

        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new FormatException("CSV is empty.");
        }

        // Accept either an "Email" header or a headerless single-column file
        // where the first row is itself an email. The latter is what teachers
        // get when they paste a list into Excel and save as CSV.
        var headerCells = SplitSimple(headerLine);
        var emailIdx = -1;
        var dataStartsAtHeader = false;

        for (var i = 0; i < headerCells.Count; i++)
        {
            if (headerCells[i].Trim().Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                emailIdx = i;
                break;
            }
        }

        if (emailIdx == -1)
        {
            // No header row found. Treat the first line as data IF it looks like an email.
            if (headerCells.Count >= 1 && headerCells[0].Contains('@'))
            {
                emailIdx = 0;
                dataStartsAtHeader = true;
            }
            else
            {
                throw new FormatException(
                    "CSV must have an 'Email' column or be a single-column list of emails.");
            }
        }

        var rows = new List<BulkEnrollRow>();
        var lineNumber = 1;

        if (dataStartsAtHeader)
        {
            rows.Add(new BulkEnrollRow { Line = 1, Email = headerCells[emailIdx].Trim() });
        }

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = SplitSimple(line);
            var email = emailIdx < cells.Count ? cells[emailIdx].Trim() : string.Empty;
            rows.Add(new BulkEnrollRow { Line = lineNumber, Email = email });
        }

        return rows;
    }

    private static List<string> SplitSimple(string line)
    {
        // Bulk-enroll only needs one column, so we don't bother with quoted
        // fields here — emails don't contain commas. Plain comma-split keeps
        // the parser obvious.
        return line.Split(',').Select(s => s.Trim('"')).ToList();
    }
}
