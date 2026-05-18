namespace LMS.Application.Teacher.Dtos;

/// <summary>One row in the course-builder "Assessments" section.</summary>
public class TeacherAssessmentListItemDto
{
    public Guid AssessmentId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    /// <summary>"Quiz" or "Assignment".</summary>
    public string Type { get; set; } = string.Empty;
    public int? TimeLimit { get; set; }
    public int PassingScore { get; set; }
    public DateTime? DueDate { get; set; }
    public int? MaxAttempts { get; set; }
    public int QuestionCount { get; set; }
    public int TotalPoints { get; set; }
    public int SubmissionCount { get; set; }
    public int UngradedCount { get; set; }
}

/// <summary>Full assessment payload for the teacher editor — includes CorrectAnswer.</summary>
public class TeacherAssessmentDto
{
    public Guid AssessmentId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? TimeLimit { get; set; }
    public int PassingScore { get; set; }
    public DateTime? DueDate { get; set; }
    public int? MaxAttempts { get; set; }
    public int TotalPoints { get; set; }
    public int SubmissionCount { get; set; }

    /// <summary>Attached grading rubric, if any (BRD TCH-025). Only present for Assignment-type.</summary>
    public Guid? RubricId { get; set; }
    public RubricDto? Rubric { get; set; }

    public IReadOnlyList<TeacherQuestionDto> Questions { get; set; } = Array.Empty<TeacherQuestionDto>();
}

public class TeacherQuestionDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    /// <summary>"MCQ" | "TrueFalse" | "ShortAnswer" | "Essay".</summary>
    public string Type { get; set; } = string.Empty;
    public IReadOnlyList<string>? Options { get; set; }
    /// <summary>Teacher view — correct answer IS exposed here. Hidden in the student DTO.</summary>
    public string? CorrectAnswer { get; set; }
    public int Points { get; set; }
    public int Order { get; set; }
}
