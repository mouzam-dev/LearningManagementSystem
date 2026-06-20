namespace LMS.Domain.Entities;

/// <summary>
/// One hadith, imported from the Sunnah.com database dump (the MySQL
/// <c>HadithTable</c>). Read-only reference data with no relationships to the
/// rest of the LMS schema — it backs the in-app hadith reader so the application
/// does not depend on api.sunnah.com at runtime.
///
/// <para>
/// <see cref="BodyEn"/>/<see cref="BodyAr"/> are stored RAW, keeping the source's
/// inline markup (<c>[narrator]</c>, <c>[prematn]</c>/<c>[matn]</c>). The read
/// service strips it for display; preserving it leaves the door open to richer
/// rendering later (e.g. narrator-hover tooltips as on sunnah.com).
/// </para>
/// </summary>
public class Hadith
{
    public int Id { get; set; }

    /// <summary>Collection slug, e.g. "bukhari".</summary>
    public string Collection { get; set; } = null!;

    /// <summary>Book number within the collection, e.g. "1".</summary>
    public string BookNumber { get; set; } = null!;

    /// <summary>Chapter (bab) id within the book — used for chapter grouping/ordering.</summary>
    public decimal ChapterId { get; set; }

    /// <summary>Display hadith number, e.g. "1" or "7a".</summary>
    public string HadithNumber { get; set; } = null!;

    /// <summary>Sunnah.com canonical ordering value within the collection.</summary>
    public int OurHadithNumber { get; set; }

    public int ArabicUrn { get; set; }
    public int EnglishUrn { get; set; }

    /// <summary>English chapter (bab) name.</summary>
    public string? ChapterEn { get; set; }

    /// <summary>Arabic chapter (bab) name.</summary>
    public string? ChapterAr { get; set; }

    /// <summary>English narration (raw markup).</summary>
    public string? BodyEn { get; set; }

    /// <summary>Arabic narration (raw markup).</summary>
    public string? BodyAr { get; set; }

    /// <summary>English grading, e.g. "Sahih" or "Hasan (Darussalam)".</summary>
    public string? GradeEn { get; set; }

    /// <summary>Arabic grading, e.g. "صحيح".</summary>
    public string? GradeAr { get; set; }

    /// <summary>
    /// Normalized grade bucket for the search filter — one of
    /// Sahih / Hasan / Daif / Maudu / Other (null when ungraded).
    /// </summary>
    public string? GradeCategory { get; set; }

    /// <summary>Denormalized book/section name (English) — for book lists + search results.</summary>
    public string? BookNameEn { get; set; }

    /// <summary>Denormalized book/section name (Arabic).</summary>
    public string? BookNameAr { get; set; }
}
