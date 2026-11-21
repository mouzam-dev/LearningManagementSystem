namespace LMS.Domain.Entities;

/// <summary>
/// One unit of the "Al-Arabiyyah Bayna Yadayk" Arabic course (4 books x ~16 units).
/// Read-only reference content seeded from JSON; Id is the source key, e.g. "b1u01".
/// </summary>
public class ArabicUnit
{
    public string Id { get; set; } = null!;
    public int BookNumber { get; set; }
    public int UnitNumber { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleUr { get; set; } = string.Empty;

    public ICollection<ArabicLesson> Lessons { get; set; } = new List<ArabicLesson>();
}
