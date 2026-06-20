using System.Text.RegularExpressions;
using LMS.Application.Common;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LMS.WebAPI.Services.Sunnah;

/// <summary>
/// Serves the hadith reader entirely from the local DB — <c>HadithCollections</c>
/// for the collection list and <c>Hadiths</c> (grouped) for books + narrations.
/// The data is rebuilt by <see cref="HadithHarvestService"/>. Collection/book lists
/// are cached; the harvester clears those keys on a successful refresh.
/// </summary>
public class DbSunnahService : ISunnahService
{
    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(6);

    public DbSunnahService(IApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<SunnahPageDto<SunnahCollectionDto>> GetCollectionsAsync(int page, int limit, CancellationToken ct)
    {
        var all = await _cache.GetOrCreateAsync(SunnahCacheKeys.Collections, async e =>
        {
            e.AbsoluteExpirationRelativeToNow = Ttl;
            return await _db.HadithCollections.AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .Select(c => new SunnahCollectionDto
                {
                    Name = c.Slug,
                    TitleEn = c.TitleEn,
                    TitleAr = c.TitleAr,
                    ShortIntroEn = c.ShortIntroEn,
                    HasBooks = true,
                    TotalHadith = c.HadithCount,
                })
                .ToListAsync(ct);
        }) ?? new List<SunnahCollectionDto>();

        return Paged(all, page, limit);
    }

    public async Task<SunnahPageDto<SunnahBookDto>> GetBooksAsync(string collection, int page, int limit, CancellationToken ct)
    {
        var all = await _cache.GetOrCreateAsync(SunnahCacheKeys.Books(collection), async e =>
        {
            e.AbsoluteExpirationRelativeToNow = Ttl;
            var groups = await _db.Hadiths.AsNoTracking()
                .Where(h => h.Collection == collection)
                .GroupBy(h => new { h.BookNumber, h.BookNameEn, h.BookNameAr })
                .Select(g => new
                {
                    g.Key.BookNumber,
                    g.Key.BookNameEn,
                    g.Key.BookNameAr,
                    Count = g.Count(),
                    Min = g.Min(x => x.OurHadithNumber),
                    Max = g.Max(x => x.OurHadithNumber),
                })
                .ToListAsync(ct);

            return groups
                .OrderBy(g => g.Min)
                .Select(g => new SunnahBookDto
                {
                    BookNumber = g.BookNumber,
                    NameEn = string.IsNullOrWhiteSpace(g.BookNameEn) ? $"Book {g.BookNumber}" : g.BookNameEn!,
                    NameAr = g.BookNameAr ?? string.Empty,
                    NumberOfHadith = g.Count,
                    HadithStartNumber = g.Min,
                    HadithEndNumber = g.Max,
                })
                .ToList();
        }) ?? new List<SunnahBookDto>();

        return Paged(all, page, limit);
    }

    public async Task<SunnahPageDto<SunnahHadithDto>> GetHadithsAsync(string collection, string bookNumber, int page, int limit, CancellationToken ct)
    {
        var query = _db.Hadiths.AsNoTracking()
            .Where(h => h.Collection == collection && h.BookNumber == bookNumber)
            .OrderBy(h => h.OurHadithNumber).ThenBy(h => h.Id);
        return await PageQuery(query, page, limit, ct);
    }

    public async Task<SunnahPageDto<SunnahHadithDto>> SearchAsync(
        string? query, string? collection, string? grade, string? bookNumber, int page, int limit, CancellationToken ct)
    {
        var q = _db.Hadiths.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(collection)) q = q.Where(h => h.Collection == collection);
        if (!string.IsNullOrWhiteSpace(grade)) q = q.Where(h => h.GradeCategory == grade);
        if (!string.IsNullOrWhiteSpace(bookNumber)) q = q.Where(h => h.BookNumber == bookNumber);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            var like = $"%{term}%";
            q = q.Where(h =>
                EF.Functions.Like(h.BodyEn!, like) ||
                EF.Functions.Like(h.BodyAr!, like) ||
                h.HadithNumber == term);
        }

        q = q.OrderBy(h => h.Collection).ThenBy(h => h.OurHadithNumber).ThenBy(h => h.Id);
        return await PageQuery(q, page, limit, ct);
    }

    public async Task<SunnahHadithDto?> GetRandomAsync(CancellationToken ct)
    {
        var total = await _db.Hadiths.CountAsync(ct);
        if (total == 0) return null;
        var h = await _db.Hadiths.AsNoTracking()
            .OrderBy(x => x.Id)
            .Skip(Random.Shared.Next(total))
            .Take(1)
            .FirstOrDefaultAsync(ct);
        return h is null ? null : Map(h);
    }

    // ---- helpers ----

    private static async Task<SunnahPageDto<SunnahHadithDto>> PageQuery(IQueryable<Hadith> query, int page, int limit, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var skip = Math.Max(0, (page - 1) * limit);
        var rows = await query.Skip(skip).Take(limit).ToListAsync(ct);
        return new SunnahPageDto<SunnahHadithDto>
        {
            Data = rows.Select(Map).ToList(),
            Total = total,
            Next = skip + rows.Count < total ? page + 1 : null,
            Previous = page > 1 ? page - 1 : null,
        };
    }

    private static SunnahHadithDto Map(Hadith h) => new()
    {
        Collection = h.Collection,
        BookNumber = h.BookNumber,
        BookNameEn = h.BookNameEn,
        HadithNumber = h.HadithNumber,
        ChapterEn = h.ChapterEn,
        ChapterAr = h.ChapterAr,
        BodyEn = h.BodyEn ?? string.Empty,
        BodyAr = h.BodyAr ?? string.Empty,
        Grade = !string.IsNullOrWhiteSpace(h.GradeEn) ? h.GradeEn : h.GradeAr,
        GradeCategory = h.GradeCategory,
    };

    private static SunnahPageDto<T> Paged<T>(List<T> all, int page, int limit)
    {
        var skip = Math.Max(0, (page - 1) * limit);
        var data = all.Skip(skip).Take(limit).ToList();
        return new SunnahPageDto<T>
        {
            Data = data,
            Total = all.Count,
            Next = skip + data.Count < all.Count ? page + 1 : null,
            Previous = page > 1 ? page - 1 : null,
        };
    }
}

/// <summary>Shared cache keys so <see cref="HadithHarvestService"/> can invalidate after a refresh.</summary>
internal static class SunnahCacheKeys
{
    public const string Collections = "sunnah:db:collections";
    public static string Books(string slug) => $"sunnah:db:books:{slug}";
}

/// <summary>
/// Strips the source's inline markup — <c>[narrator ...]</c>, <c>[prematn]</c>/<c>[matn]</c> —
/// keeping inner text + any HTML the SPA renders. Used at harvest time.
/// </summary>
internal static class SunnahMarkup
{
    private static readonly Regex Tag = new(@"\[/?[a-zA-Z][^\]]*\]", RegexOptions.Compiled);

    public static string? Clean(string? raw) =>
        string.IsNullOrEmpty(raw) ? raw : Tag.Replace(raw, string.Empty).Trim();
}
