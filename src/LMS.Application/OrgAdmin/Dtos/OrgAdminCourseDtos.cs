namespace LMS.Application.OrgAdmin.Dtos;

/// <summary>One row in the OrgAdmin's course moderation list. Only includes
/// courses whose denormalized <c>OrganizationId</c> matches the caller's org.</summary>
public class OrgAdminCourseListItemDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int ModuleCount { get; set; }
    public int LessonCount { get; set; }
    public int StudentCount { get; set; }
    public int AssessmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class OrgAdminCoursesPage
{
    public IReadOnlyList<OrgAdminCourseListItemDto> Items { get; set; } = Array.Empty<OrgAdminCourseListItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class OrgAdminCourseDetailDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? MaxStudents { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }

    public int ModuleCount { get; set; }
    public int LessonCount { get; set; }
    public int AssessmentCount { get; set; }
    public int StudentCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal AverageProgress { get; set; }
}
