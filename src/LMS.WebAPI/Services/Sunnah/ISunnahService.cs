namespace LMS.WebAPI.Services.Sunnah;

/// <summary>Read API for the hadith reader — served from the local DB by <see cref="DbSunnahService"/>.</summary>
public interface ISunnahService
{
    Task<SunnahPageDto<SunnahCollectionDto>> GetCollectionsAsync(int page, int limit, CancellationToken ct);
    Task<SunnahPageDto<SunnahBookDto>> GetBooksAsync(string collection, int page, int limit, CancellationToken ct);
    Task<SunnahPageDto<SunnahHadithDto>> GetHadithsAsync(string collection, string bookNumber, int page, int limit, CancellationToken ct);

    /// <summary>Full-text + faceted search across all hadith (text, collection, grade bucket, book).</summary>
    Task<SunnahPageDto<SunnahHadithDto>> SearchAsync(
        string? query, string? collection, string? grade, string? bookNumber, int page, int limit, CancellationToken ct);

    Task<SunnahHadithDto?> GetRandomAsync(CancellationToken ct);
}
