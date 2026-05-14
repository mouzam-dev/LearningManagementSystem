namespace LMS.Application.Teacher.Dtos;

/// <summary>Full payload for the course-builder page.</summary>
public class TeacherCourseDetailDto
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

    public int StudentCount { get; set; }
    public int AssessmentCount { get; set; }
    public decimal AverageProgress { get; set; }

    public IReadOnlyList<TeacherModuleDto> Modules { get; set; } = Array.Empty<TeacherModuleDto>();
    public IReadOnlyList<TeacherAssessmentListItemDto> Assessments { get; set; } = Array.Empty<TeacherAssessmentListItemDto>();
}

public class TeacherModuleDto
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public IReadOnlyList<TeacherLessonDto> Lessons { get; set; } = Array.Empty<TeacherLessonDto>();
}

public class TeacherLessonDto
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    /// <summary>"Video" | "Document" | "Text" | etc.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Raw JSON content payload (already-serialized by the caller, parsed by the consumer).</summary>
    public string? Content { get; set; }
    public int? Duration { get; set; }
    public int Order { get; set; }
    public bool IsPublished { get; set; }
}
