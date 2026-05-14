namespace LMS.Application.Admin.Dtos;

/// <summary>One row in the admin's platform-wide course list.</summary>
public class AdminCourseListItemDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public int ModuleCount { get; set; }
    public int LessonCount { get; set; }
    public int StudentCount { get; set; }
    public int AssessmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdminCoursesPage
{
    public IReadOnlyList<AdminCourseListItemDto> Items { get; set; } = Array.Empty<AdminCourseListItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>Admin's read-mostly course detail — oversight, not authoring.</summary>
public class AdminCourseDetailDto
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

    public int ModuleCount { get; set; }
    public int LessonCount { get; set; }
    public int AssessmentCount { get; set; }
    public int StudentCount { get; set; }
    public int CompletedCount { get; set; }
    public int CertificatesIssued { get; set; }
    public decimal AverageProgress { get; set; }

    public IReadOnlyList<AdminCourseModuleDto> Modules { get; set; } = Array.Empty<AdminCourseModuleDto>();
}

public class AdminCourseModuleDto
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public int LessonCount { get; set; }
}

// ---------------- Audit log ------------------------------------------------

public class AdminAuditLogDto
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Changes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminAuditLogPage
{
    public IReadOnlyList<AdminAuditLogDto> Items { get; set; } = Array.Empty<AdminAuditLogDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
