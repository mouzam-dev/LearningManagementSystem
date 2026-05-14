namespace LMS.Application.Teacher.Dtos;

/// <summary>One row in the teacher's grading inbox.</summary>
public class GradingQueueItemDto
{
    public Guid SubmissionId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int? Score { get; set; }
    public DateTime? GradedAt { get; set; }
    public int TotalPoints { get; set; }
    public int PassingScore { get; set; }
}

/// <summary>Full submission payload for the grader page.</summary>
public class TeacherSubmissionDetailDto
{
    public Guid SubmissionId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public int? Score { get; set; }
    public string? Feedback { get; set; }

    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public int PassingScore { get; set; }
    public int TotalPoints { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;

    /// <summary>For Quiz submissions: per-question student answer + correctness + reference correct answer.</summary>
    public IReadOnlyList<SubmissionAnswerDto> Answers { get; set; } = Array.Empty<SubmissionAnswerDto>();

    /// <summary>For Assignment submissions: the single free-text response (or null).</summary>
    public string? AssignmentResponse { get; set; }
}

public class SubmissionAnswerDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? StudentAnswer { get; set; }
    public string? CorrectAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public int Points { get; set; }
    public int Order { get; set; }
}
