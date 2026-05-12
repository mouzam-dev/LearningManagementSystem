namespace LMS.Application.Student.Dtos;

public class LessonDetailDto
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    /// <summary>Duration in minutes.</summary>
    public int? Duration { get; set; }
    public int Order { get; set; }

    /// <summary>Parsed from Lesson.Content JSON (e.g. {"videoUrl": "..."}).</summary>
    public string? VideoUrl { get; set; }
    /// <summary>Optional rich body for non-video lesson types. Currently null for Video.</summary>
    public string? Body { get; set; }

    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public Guid ModuleId { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;

    public int WatchTimeSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid? PreviousLessonId { get; set; }
    public Guid? NextLessonId { get; set; }

    /// <summary>Full course syllabus so the player can render its sidebar
    /// without a second round-trip.</summary>
    public IReadOnlyList<ModuleDto> Modules { get; set; } = Array.Empty<ModuleDto>();
}
