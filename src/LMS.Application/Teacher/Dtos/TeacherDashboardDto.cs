namespace LMS.Application.Teacher.Dtos;

public class TeacherDashboardDto
{
    public TeacherDashboardSummary Summary { get; set; } = new();
    public IReadOnlyList<TeacherCourseSummary> TopCourses { get; set; } = Array.Empty<TeacherCourseSummary>();
    public IReadOnlyList<RecentEnrollmentDto> RecentEnrollments { get; set; } = Array.Empty<RecentEnrollmentDto>();
    public IReadOnlyList<PendingGradingDto> PendingGrading { get; set; } = Array.Empty<PendingGradingDto>();
}

public class TeacherDashboardSummary
{
    public int TotalCourses { get; set; }
    public int PublishedCourses { get; set; }
    public int DraftCourses { get; set; }
    public int TotalStudents { get; set; }
    public int TotalActiveStudents { get; set; }
    public int CompletedEnrollments { get; set; }
    public int PendingGradingCount { get; set; }
    public int CertificatesIssued { get; set; }
}

/// <summary>One row in the "Top courses" panel — owned course w/ student count.</summary>
public class TeacherCourseSummary
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public int StudentCount { get; set; }
    public int LessonCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RecentEnrollmentDto
{
    public Guid EnrollmentId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
}

public class PendingGradingDto
{
    public Guid SubmissionId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
