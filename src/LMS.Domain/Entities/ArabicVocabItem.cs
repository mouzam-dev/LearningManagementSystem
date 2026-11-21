namespace LMS.Domain.Entities;

/// <summary>A new word introduced in a lesson.</summary>
public class ArabicVocabItem
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public int Order { get; set; }
    public string Ar { get; set; } = string.Empty;
    public string Tr { get; set; } = string.Empty;
    public string En { get; set; } = string.Empty;
    public string Ur { get; set; } = string.Empty;
    /// <summary>Audio clip file name (served from /learn-arabic/audio/); null when there is none.</summary>
    public string? Audio { get; set; }

    public ArabicLesson Lesson { get; set; } = null!;
}
