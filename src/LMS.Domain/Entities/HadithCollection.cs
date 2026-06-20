namespace LMS.Domain.Entities;

/// <summary>
/// Display metadata for a hadith collection (title, intro, ordering), populated by
/// the harvest so the collection list is fully DB-driven and updates on every
/// "Refresh from source". Separate from <see cref="Hadith"/> (the narration rows).
/// </summary>
public class HadithCollection
{
    /// <summary>Collection slug — primary key, e.g. "bukhari".</summary>
    public string Slug { get; set; } = null!;

    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? ShortIntroEn { get; set; }

    /// <summary>Display order (canonical sunnah.com ordering).</summary>
    public int SortOrder { get; set; }

    /// <summary>Where the data came from: "sunnah" or "fawaz".</summary>
    public string Source { get; set; } = "sunnah";

    /// <summary>Hadith count at last harvest (denormalized for the list view).</summary>
    public int HadithCount { get; set; }
}
