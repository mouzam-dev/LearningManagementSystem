namespace LMS.Application.Teacher.Dtos;

/// <summary>One row in the teacher's "My courses" list page.</summary>
public class TeacherCourseListItemDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? MaxStudents { get; set; }
    public bool IsPublished { get; set; }

    public int ModuleCount { get; set; }
    public int LessonCount { get; set; }
    public int AssessmentCount { get; set; }
    public int StudentCount { get; set; }
    public decimal AverageProgress { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Returned to the client after a successful POST /api/teacher/courses.</summary>
public class CreatedCourseDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? MaxStudents { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}
