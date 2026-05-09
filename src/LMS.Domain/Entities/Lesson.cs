namespace LMS.Domain.Entities;

public class Lesson
{
    public Guid Id { get; set; }
    public Guid ModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Video" | "Document" | "Text" | "Quiz" | "Assignment"
    public string? Content { get; set; }              // JSON payload (video URL, document URL, text body, quiz id, etc.)
    public int? Duration { get; set; }                // minutes
    public int Order { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Module Module { get; set; } = null!;
    public ICollection<LessonProgress> Progress { get; set; } = new List<LessonProgress>();
}
