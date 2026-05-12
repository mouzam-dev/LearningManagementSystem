namespace LMS.Application.Student.Dtos;

/// <summary>One row in a course's assessment list.</summary>
public class AssessmentListItemDto
{
    public Guid AssessmentId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    /// <summary>"Quiz" or "Assignment".</summary>
    public string Type { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    /// <summary>Time limit in minutes (null = untimed).</summary>
    public int? TimeLimit { get; set; }
    public int PassingScore { get; set; }
    public int? MaxAttempts { get; set; }
    public int QuestionCount { get; set; }
    public int AttemptCount { get; set; }
    public int? BestScore { get; set; }
    public bool IsPassed { get; set; }
}

/// <summary>Full assessment payload sent to the taker page (no correct answers!).</summary>
public class AssessmentDetailDto
{
    public Guid AssessmentId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? TimeLimit { get; set; }
    public int PassingScore { get; set; }
    public int TotalPoints { get; set; }
    public DateTime? DueDate { get; set; }
    public int? MaxAttempts { get; set; }
    public int AttemptCount { get; set; }
    public int? AttemptsRemaining { get; set; }

    public IReadOnlyList<QuestionDto> Questions { get; set; } = Array.Empty<QuestionDto>();
    public SubmissionResultDto? LastSubmission { get; set; }
}

public class QuestionDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    /// <summary>"MCQ" | "TrueFalse" | "ShortAnswer" | "Essay".</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Choice list for MCQ / TrueFalse. Null for free-response types.</summary>
    public IReadOnlyList<string>? Options { get; set; }
    public int Points { get; set; }
    public int Order { get; set; }
}

/// <summary>What the server returns immediately after a submission.</summary>
public class SubmissionResultDto
{
    public Guid SubmissionId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentType { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public int? Score { get; set; }
    public int TotalPoints { get; set; }
    public int? ScorePercentage { get; set; }
    public bool IsPassed { get; set; }
    public string? Feedback { get; set; }
    /// <summary>Per-question correctness for auto-graded quiz submissions.
    /// Null for assignments that haven't been manually graded.</summary>
    public IReadOnlyList<QuestionResultDto>? QuestionResults { get; set; }
}

public class QuestionResultDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? StudentAnswer { get; set; }
    public string? CorrectAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public int PointsAwarded { get; set; }
    public int PointsPossible { get; set; }
}
