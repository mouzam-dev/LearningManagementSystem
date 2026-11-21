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

    public ArabicLesson Lesson { get; set; } = null!;
}
