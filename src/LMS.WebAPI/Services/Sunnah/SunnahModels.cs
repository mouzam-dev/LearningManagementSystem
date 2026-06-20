namespace LMS.WebAPI.Services.Sunnah;

// Flattened DTOs returned to the SPA (the raw API nests en/ar in arrays).

public class SunnahCollectionDto
{
    public string Name { get; set; } = string.Empty; // "bukhari"
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? ShortIntroEn { get; set; }
    public bool HasBooks { get; set; }
    public int TotalHadith { get; set; }
}

public class SunnahBookDto
{
    public string BookNumber { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int NumberOfHadith { get; set; }
    public int HadithStartNumber { get; set; }
    public int HadithEndNumber { get; set; }
}

public class SunnahHadithDto
{
    public string Collection { get; set; } = string.Empty;
    public string BookNumber { get; set; } = string.Empty;
    public string? BookNameEn { get; set; }
    public string HadithNumber { get; set; } = string.Empty;
    public string? ChapterEn { get; set; }
    public string? ChapterAr { get; set; }
    public string BodyEn { get; set; } = string.Empty; // HTML
    public string BodyAr { get; set; } = string.Empty; // HTML
    public string? Grade { get; set; }
    public string? GradeCategory { get; set; } // Sahih/Hasan/Daif/Maudu/Other
}

public class SunnahPageDto<T>
{
    public List<T> Data { get; set; } = new();
    public int Total { get; set; }
    public int? Next { get; set; }
    public int? Previous { get; set; }
}
