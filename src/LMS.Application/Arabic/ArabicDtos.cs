using System.Text.Json.Serialization;

namespace LMS.Application.Arabic;

// The catalog DTOs mirror the learn-app's data format (window.CATALOG), so the embedded app at
// /learn-arabic/ reads them unchanged: books -> units -> lessons -> dialogue / vocab / quiz.

public record ArabicTextDto(string Ar, string En, string Ur);

/// <summary>A dialogue line. Src: "quran" | "hadith" | "poem" for exact quotations; Au: audio clip file name.</summary>
public record ArabicCatalogLineDto(string Who, string Ar, string Tr, string En, string Ur, string? Src, string? Au);

public record ArabicCatalogWordDto(string Ar, string Tr, string En, string Ur, string? Au);

public record ArabicCatalogQuizDto(
    ArabicTextDto Q,
    IReadOnlyList<string> Options,
    [property: JsonPropertyName("options_ar")] IReadOnlyList<string>? OptionsAr,
    int Answer);

public record ArabicCatalogLessonDto(int N, ArabicTextDto Title, ArabicTextDto Intro,
    IReadOnlyList<ArabicCatalogLineDto> Dialogue, IReadOnlyList<ArabicCatalogWordDto> Vocab, IReadOnlyList<ArabicCatalogQuizDto> Quiz);

public record ArabicCatalogUnitDto(string Id, int Book, int Unit, ArabicTextDto Title, IReadOnlyList<ArabicCatalogLessonDto> Lessons);

public record ArabicCatalogBookDto(int N, IReadOnlyList<ArabicCatalogUnitDto> Units);

public record ArabicProgressDto(string UnitId, int LessonNumber, int BestScore, int Attempts, DateTime UpdatedAt);
