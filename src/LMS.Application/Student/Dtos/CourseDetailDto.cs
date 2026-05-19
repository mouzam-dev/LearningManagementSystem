namespace LMS.Application.Student.Dtos;

public class CourseDetailDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int EnrolledCount { get; set; }
    public int? MaxStudents { get; set; }

    public bool IsEnrolled { get; set; }
    /// <summary>0-100, average across all lessons in the course.</summary>
    public decimal ProgressPercentage { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }

    public IReadOnlyList<ModuleDto> Modules { get; set; } = Array.Empty<ModuleDto>();

    /// <summary>Lesson the "Continue learning" button should jump to — first
    /// incomplete lesson in syllabus order, or null if everything is done /
    /// the course has no lessons yet.</summary>
    public Guid? NextLessonId { get; set; }
}

public class ModuleDto
{
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public IReadOnlyList<LessonListItemDto> Lessons { get; set; } = Array.Empty<LessonListItemDto>();
}

public class LessonListItemDto
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    /// <summary>Duration in minutes.</summary>
    public int? Duration { get; set; }
    public int Order { get; set; }
    public bool IsCompleted { get; set; }
    public int WatchTimeSeconds { get; set; }
}
