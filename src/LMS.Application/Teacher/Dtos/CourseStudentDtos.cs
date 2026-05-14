namespace LMS.Application.Teacher.Dtos;

/// <summary>One row in the per-course student roster.</summary>
public class CourseStudentListItemDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public decimal ProgressPercentage { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int LessonsCompleted { get; set; }
    public int TotalLessons { get; set; }
    public int SubmissionsCount { get; set; }
    public int? AverageScore { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool HasCertificate { get; set; }
}

/// <summary>Drill-in payload for a single student in a course.</summary>
public class CourseStudentDetailDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public decimal ProgressPercentage { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }

    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public Guid? CertificateId { get; set; }
    public string? CertificateVerifyCode { get; set; }

    public IReadOnlyList<StudentLessonProgressDto> Lessons { get; set; } = Array.Empty<StudentLessonProgressDto>();
    public IReadOnlyList<StudentSubmissionDto> Submissions { get; set; } = Array.Empty<StudentSubmissionDto>();
}

public class StudentLessonProgressDto
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string ModuleTitle { get; set; } = string.Empty;
    public int ModuleOrder { get; set; }
    public int LessonOrder { get; set; }
    public bool IsCompleted { get; set; }
    public int WatchTimeSeconds { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class StudentSubmissionDto
{
    public Guid SubmissionId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public int PassingScore { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public int? Score { get; set; }
    public bool IsPassed { get; set; }
}

/// <summary>Aggregate analytics for the entire course.</summary>
public class CourseAnalyticsDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;

    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int CompletedStudents { get; set; }
    public decimal AverageProgress { get; set; }
    public int CompletionRate { get; set; }       // percent of enrolled who finished
    public int CertificatesIssued { get; set; }

    public int TotalLessons { get; set; }
    public int TotalAssessments { get; set; }
    public int TotalSubmissions { get; set; }
    public int UngradedSubmissions { get; set; }

    /// <summary>Per-lesson completion rate. Surfaces "stuck" lessons where many students drop off.</summary>
    public IReadOnlyList<LessonAnalyticsDto> LessonStats { get; set; } = Array.Empty<LessonAnalyticsDto>();

    /// <summary>Per-assessment summary. Lets teachers see which quizzes are too easy or too hard.</summary>
    public IReadOnlyList<AssessmentAnalyticsDto> AssessmentStats { get; set; } = Array.Empty<AssessmentAnalyticsDto>();

    /// <summary>Top 5 students by progress, then by enrolment age.</summary>
    public IReadOnlyList<StudentMiniDto> TopStudents { get; set; } = Array.Empty<StudentMiniDto>();

    /// <summary>Up to 5 students with low progress + no activity in 14+ days.</summary>
    public IReadOnlyList<StudentMiniDto> AtRiskStudents { get; set; } = Array.Empty<StudentMiniDto>();
}

public class LessonAnalyticsDto
{
    public Guid LessonId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string ModuleTitle { get; set; } = string.Empty;
    public int ModuleOrder { get; set; }
    public int LessonOrder { get; set; }
    public int CompletedCount { get; set; }
    public int CompletionRate { get; set; }       // percent of enrolled who finished this lesson
}

public class AssessmentAnalyticsDto
{
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string AssessmentType { get; set; } = string.Empty;
    public int PassingScore { get; set; }
    public int SubmissionCount { get; set; }
    public int? AverageScore { get; set; }
    public int PassRate { get; set; }             // percent of submissions that passed
    public int UngradedCount { get; set; }
}

public class StudentMiniDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
    public DateTime? LastActivityAt { get; set; }
}
