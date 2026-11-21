namespace LMS.Domain.Entities;

/// <summary>One spoken line of a lesson dialogue (Arabic + transliteration + English/Urdu meaning).</summary>
public class ArabicDialogueLine
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public int Order { get; set; }
    public string Speaker { get; set; } = string.Empty;
    public string Ar { get; set; } = string.Empty;
    public string Tr { get; set; } = string.Empty;
    public string En { get; set; } = string.Empty;
    public string Ur { get; set; } = string.Empty;
    /// <summary>"quran", "hadith" or "poem" when the line quotes one exactly; null for ordinary dialogue.</summary>
    public string? Source { get; set; }
    /// <summary>Audio clip file name (served from /learn-arabic/audio/); null when there is none.</summary>
    public string? Audio { get; set; }

    public ArabicLesson Lesson { get; set; } = null!;
}
