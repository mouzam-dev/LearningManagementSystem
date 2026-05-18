namespace LMS.Application.Teacher.Dtos;

/// <summary>One CSV row from the bulk-enroll upload (single Email column).</summary>
public class BulkEnrollRow
{
    public int Line { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class BulkEnrollRowResultDto
{
    public int Line { get; set; }
    public string Email { get; set; } = string.Empty;
    /// <summary>"Enrolled" (new), "Skipped" (already enrolled), or "Failed".</summary>
    public string Status { get; set; } = string.Empty;
    public Guid? StudentId { get; set; }
    public string? Error { get; set; }
}

public class BulkEnrollResultDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int EnrolledCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailureCount { get; set; }
    public int CourseEnrolledTotal { get; set; }
    public int? CourseMaxStudents { get; set; }
    public IReadOnlyList<BulkEnrollRowResultDto> Rows { get; set; } = Array.Empty<BulkEnrollRowResultDto>();
}
