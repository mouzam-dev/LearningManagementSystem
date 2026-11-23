namespace LMS.Domain.Entities;

/// <summary>A surah of the Qur'an. Read-only reference content seeded from Data/quran.json.</summary>
public class QuranSurah
{
    public int Number { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameTransliterated { get; set; } = string.Empty;
    public string RevelationPlace { get; set; } = string.Empty;
    public int VerseCount { get; set; }

    public ICollection<QuranVerse> Verses { get; set; } = new List<QuranVerse>();
}
