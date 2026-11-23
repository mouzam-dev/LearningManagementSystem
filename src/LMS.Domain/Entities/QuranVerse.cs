namespace LMS.Domain.Entities;

/// <summary>
/// One ayah: Uthmani Arabic plus the English (Saheeh International) and Urdu (Jalandhari)
/// translations. The table also has a database-generated full-text search column (see migration).
/// </summary>
public class QuranVerse
{
    public int Id { get; set; }
    public int SurahNumber { get; set; }
    public int VerseNumber { get; set; }
    public string TextAr { get; set; } = string.Empty;
    public string TextEn { get; set; } = string.Empty;
    public string TextUr { get; set; } = string.Empty;

    public QuranSurah Surah { get; set; } = null!;
}
