using System.Net.Http.Json;
using LMS.Domain.Entities;
using LMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LMS.WebAPI.Services.Sunnah;

/// <summary>
/// Rebuilds the local hadith tables from the source APIs:
///  • most collections from Sunnah.com (en + ar + grades), throttled with retry;
///  • Muwatta Malik from fawazahmed0's CDN (Sunnah.com's API exposes no books for it).
/// Runs as a fire-and-forget background job; per-collection replace keeps the reader
/// usable while it runs, and out-of-scope collections are pruned ("clean + refresh").
/// </summary>
public class HadithHarvestService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpFactory;
    private readonly HadithHarvestStatus _status;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HadithHarvestService> _logger;

    // Be gentle with Sunnah.com's rate limiter (it returns 502 under bursts).
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(450);

    private Dictionary<string, (string TitleEn, string TitleAr, string? Intro)> _colMeta = new();

    public HadithHarvestService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpFactory,
        HadithHarvestStatus status,
        IMemoryCache cache,
        ILogger<HadithHarvestService> logger)
    {
        _scopeFactory = scopeFactory;
        _httpFactory = httpFactory;
        _status = status;
        _cache = cache;
        _logger = logger;
    }

    private HttpClient Sunnah => _httpFactory.CreateClient("sunnah");
    private HttpClient Fawaz => _httpFactory.CreateClient("fawaz");

    /// <summary>Launches the harvest in the background. Returns false if one is already running.</summary>
    public bool Start()
    {
        if (_status.State == HarvestState.Running) return false;
        _ = Task.Run(() => RunAsync(CancellationToken.None));
        return true;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (!_status.TryBegin(0)) return; // already running
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            // Scope = whatever Sunnah.com currently offers, in its order, so new books arrive without a code change.
            // Malik is kept even at 0: Sunnah.com lists it without books, so it falls back to fawazahmed0 below.
            var list = await GetAsync<RawPage<RawCollection>>(Sunnah, "collections?limit=100", ct);
            if (list?.Data is not { Count: > 0 })
                throw new InvalidOperationException("Could not load the collection list from Sunnah.com; existing data kept.");
            _colMeta = list.Data.ToDictionary(
                c => c.Name,
                c => (Lang(c.Collection, "en")?.Title ?? Humanize(c.Name),
                      Lang(c.Collection, "ar")?.Title ?? string.Empty,
                      Lang(c.Collection, "en")?.ShortIntro));
            var slugs = list.Data.Where(c => c.TotalAvailableHadith > 0 || c.Name == "malik").Select(c => c.Name).ToList();
            _status.SetTotal(slugs.Count);
            _logger.LogInformation("Hadith harvest started ({Count} collections)", slugs.Count);

            var failed = new List<string>();
            for (var i = 0; i < slugs.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var slug = slugs[i];
                _status.SetCurrent(slug);
                try
                {
                    var written = await HarvestSunnahAsync(slug, i + 1, db, ct);
                    if (written == 0 && slug == "malik") written = await HarvestMalikAsync(i + 1, db, ct);
                    _status.AddWritten(written);
                    _logger.LogInformation("Harvested {Slug}: {Written} hadith", slug, written);
                }
                catch (HttpRequestException ex)
                {
                    // A failed download must not wipe the collection: keep its previous rows and move on.
                    failed.Add(slug);
                    _logger.LogWarning(ex, "Hadith harvest skipped {Slug}; previous data kept", slug);
                }
                _status.CompleteCollection();
            }

            // Prune collections Sunnah.com no longer offers (e.g. the old dump's leftovers).
            await db.Hadiths.Where(h => !slugs.Contains(h.Collection)).ExecuteDeleteAsync(ct);
            await db.HadithCollections.Where(c => !slugs.Contains(c.Slug)).ExecuteDeleteAsync(ct);

            // Drop the read caches so the reader reflects the refresh immediately.
            _cache.Remove(SunnahCacheKeys.Collections);
            foreach (var slug in slugs) _cache.Remove(SunnahCacheKeys.Books(slug));

            _status.Finish(failed.Count == 0 ? null : $"Download failed for {string.Join(", ", failed)}; their previous data was kept.");
            _logger.LogInformation("Hadith harvest completed: {Total} hadith", _status.HadithsWritten);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hadith harvest failed");
            _status.Finish(ex.Message);
        }
    }

    // ---- Sunnah.com ----

    private async Task<int> HarvestSunnahAsync(string slug, int order, ApplicationDbContext db, CancellationToken ct)
    {
        var meta = _colMeta.TryGetValue(slug, out var m) ? m : (Humanize(slug), string.Empty, null);
        var booksPage = await GetAsync<RawPage<RawBook>>(Sunnah, $"collections/{slug}/books?limit=300", ct)
            ?? throw new HttpRequestException($"Book list for {slug} could not be downloaded.");
        var books = booksPage.Data.Where(b => b.NumberOfHadith > 0).ToList();

        const int pageSize = 100;
        var rows = new List<Hadith>();
        var seq = 0;
        var skipped = 0;
        foreach (var b in books)
        {
            var bookEn = Lang(b.Book, "en")?.Name ?? $"Book {b.BookNumber}";
            var bookAr = Lang(b.Book, "ar")?.Name ?? string.Empty;
            var url = $"collections/{slug}/books/{b.BookNumber}/hadiths";
            for (var page = 1; ; page++)
            {
                var p = await GetAsync<RawPage<RawHadith>>(Sunnah, $"{url}?page={page}&limit={pageSize}", ct)
                    ?? throw new HttpRequestException($"{slug} book {b.BookNumber} page {page} could not be downloaded.");
                var got = p.Data;
                if (p.Error is not null)
                {
                    // Sunnah.com answers 200 + {"error":…} for any page holding a broken record.
                    // Re-fetch that page one hadith at a time so only the broken records are lost.
                    got = new List<RawHadith>();
                    var last = Math.Min(page * pageSize, b.NumberOfHadith);
                    for (var i = (page - 1) * pageSize + 1; i <= last; i++)
                    {
                        var one = await GetAsync<RawPage<RawHadith>>(Sunnah, $"{url}?page={i}&limit=1", ct)
                            ?? throw new HttpRequestException($"{slug} book {b.BookNumber} hadith {i} could not be downloaded.");
                        if (one.Error is null && one.Data.Count > 0) got.AddRange(one.Data);
                        else skipped++;
                    }
                }
                foreach (var h in got)
                {
                    var en = Lang(h.Hadith, "en");
                    var ar = Lang(h.Hadith, "ar");
                    var grade = en?.Grades?.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g.Grade))?.Grade;
                    rows.Add(new Hadith
                    {
                        Collection = slug,
                        BookNumber = h.BookNumber,
                        BookNameEn = bookEn,
                        BookNameAr = bookAr,
                        HadithNumber = h.HadithNumber,
                        OurHadithNumber = ++seq,
                        ChapterEn = en?.ChapterTitle,
                        ChapterAr = ar?.ChapterTitle,
                        BodyEn = SunnahMarkup.Clean(en?.Body) ?? string.Empty,
                        BodyAr = SunnahMarkup.Clean(ar?.Body) ?? string.Empty,
                        GradeEn = grade,
                        GradeCategory = GradeBucket.Of(grade),
                    });
                }
                // The error body carries no paging info, so stop by the book's declared size instead.
                if (p.Error is not null ? page * pageSize >= b.NumberOfHadith : p.Next is null || p.Data.Count == 0) break;
            }
        }

        if (skipped > 0)
            _logger.LogWarning("{Slug}: {Skipped} hadith skipped (Sunnah.com returned an error for them)", slug, skipped);
        if (rows.Count == 0) return 0; // nothing published (yet): leave any existing rows alone
        await ReplaceCollectionAsync(db, slug, rows, ct);
        await UpsertCollectionAsync(db, slug, meta.Item1, meta.Item2, meta.Item3, order, "sunnah", rows.Count, ct);
        return rows.Count;
    }

    // ---- fawazahmed0 (Muwatta Malik) ----

    private async Task<int> HarvestMalikAsync(int order, ApplicationDbContext db, CancellationToken ct)
    {
        var en = await GetAsync<FawazEdition>(Fawaz, "editions/eng-malik.json", ct)
            ?? throw new HttpRequestException("Muwatta Malik (English) could not be downloaded.");
        var ar = await GetAsync<FawazEdition>(Fawaz, "editions/ara-malik.json", ct)
            ?? throw new HttpRequestException("Muwatta Malik (Arabic) could not be downloaded.");
        if (en.Hadiths.Count == 0) return 0;

        var arMap = (ar?.Hadiths ?? new()).ToDictionary(h => h.HadithNumber, h => h);
        var sectionsEn = en.Metadata?.Sections ?? new();
        var sectionsAr = ar?.Metadata?.Sections ?? new();

        var rows = new List<Hadith>();
        var seq = 0;
        foreach (var h in en.Hadiths)
        {
            var book = (h.Reference?.Book ?? 1).ToString();
            arMap.TryGetValue(h.HadithNumber, out var a);
            var grade = h.Grades?.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g.Grade))?.Grade;
            rows.Add(new Hadith
            {
                Collection = "malik",
                BookNumber = book,
                BookNameEn = sectionsEn.TryGetValue(book, out var bn) && !string.IsNullOrWhiteSpace(bn) ? bn : $"Book {book}",
                BookNameAr = sectionsAr.TryGetValue(book, out var ba) ? ba : string.Empty,
                HadithNumber = h.HadithNumber.ToString(),
                OurHadithNumber = ++seq,
                BodyEn = SunnahMarkup.Clean(h.Text) ?? string.Empty,
                BodyAr = SunnahMarkup.Clean(a?.Text) ?? string.Empty,
                GradeEn = grade,
                GradeCategory = GradeBucket.Of(grade),
            });
        }

        await ReplaceCollectionAsync(db, "malik", rows, ct);
        await UpsertCollectionAsync(db, "malik", en.Metadata?.Name ?? "Muwatta Malik", "موطأ مالك", null, order, "fawaz", rows.Count, ct);
        return rows.Count;
    }

    // ---- persistence ----

    private static async Task ReplaceCollectionAsync(ApplicationDbContext db, string slug, List<Hadith> rows, CancellationToken ct)
    {
        await db.Hadiths.Where(h => h.Collection == slug).ExecuteDeleteAsync(ct);
        const int batch = 1000;
        for (var i = 0; i < rows.Count; i += batch)
        {
            db.Hadiths.AddRange(rows.GetRange(i, Math.Min(batch, rows.Count - i)));
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
    }

    private static async Task UpsertCollectionAsync(ApplicationDbContext db, string slug, string titleEn,
        string titleAr, string? intro, int order, string source, int count, CancellationToken ct)
    {
        var existing = await db.HadithCollections.FirstOrDefaultAsync(x => x.Slug == slug, ct);
        if (existing is null)
        {
            existing = new HadithCollection { Slug = slug };
            db.HadithCollections.Add(existing);
        }
        existing.TitleEn = titleEn;
        existing.TitleAr = titleAr;
        existing.ShortIntroEn = intro;
        existing.SortOrder = order;
        existing.Source = source;
        existing.HadithCount = count;
        db.ChangeTracker.DetectChanges(); // auto-detect is off for the bulk insert; without this, edits to an existing row are never saved
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
    }

    // ---- HTTP helper: throttle + retry/backoff ----

    private async Task<T?> GetAsync<T>(HttpClient client, string url, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Task.Delay(MinInterval, ct);
            try
            {
                using var resp = await client.GetAsync(url, ct);
                if (resp.IsSuccessStatusCode)
                    return await resp.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                if ((int)resp.StatusCode == 429 || (int)resp.StatusCode >= 500)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1500 * (attempt + 1)), ct);
                    continue;
                }
                resp.EnsureSuccessStatusCode();
            }
            catch (Exception) when (attempt < 4)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1500 * (attempt + 1)), ct);
            }
        }
        return default;
    }

    private static RawLang? Lang(List<RawLang> langs, string code) =>
        langs.FirstOrDefault(x => string.Equals(x.Lang, code, StringComparison.OrdinalIgnoreCase));

    private static string Humanize(string slug) =>
        string.IsNullOrEmpty(slug) ? slug : char.ToUpperInvariant(slug[0]) + slug[1..];

    // ---- raw API shapes (System.Net.Http.Json: camelCase, case-insensitive) ----

    private class RawPage<T>
    {
        public List<T> Data { get; set; } = new();
        public int Total { get; set; }
        public int? Next { get; set; }
        public int? Previous { get; set; }
        public object? Error { get; set; }
    }

    private class RawLang
    {
        public string Lang { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Name { get; set; }
        public string? ShortIntro { get; set; }
        public string? ChapterTitle { get; set; }
        public string? Body { get; set; }
        public List<RawGrade>? Grades { get; set; }
    }

    private class RawGrade
    {
        public string? Name { get; set; }
        public string? Grade { get; set; }
    }

    private class RawCollection
    {
        public string Name { get; set; } = string.Empty;
        public List<RawLang> Collection { get; set; } = new();
        public int TotalHadith { get; set; }
        public int TotalAvailableHadith { get; set; }
    }

    private class RawBook
    {
        public string BookNumber { get; set; } = string.Empty;
        public List<RawLang> Book { get; set; } = new();
        public int NumberOfHadith { get; set; }
    }

    private class RawHadith
    {
        public string Collection { get; set; } = string.Empty;
        public string BookNumber { get; set; } = string.Empty;
        public string HadithNumber { get; set; } = string.Empty;
        public List<RawLang> Hadith { get; set; } = new();
    }

    // ---- fawazahmed0 shapes ----

    private class FawazEdition
    {
        public FawazMeta? Metadata { get; set; }
        public List<FawazHadith> Hadiths { get; set; } = new();
    }

    private class FawazMeta
    {
        public string? Name { get; set; }
        public Dictionary<string, string> Sections { get; set; } = new();
    }

    private class FawazHadith
    {
        public int HadithNumber { get; set; }
        public string? Text { get; set; }
        public List<RawGrade>? Grades { get; set; }
        public FawazRef? Reference { get; set; }
    }

    private class FawazRef
    {
        public int? Book { get; set; }
        public int? Hadith { get; set; }
    }
}

/// <summary>Maps a free-text grade to a coarse bucket for the search filter.</summary>
internal static class GradeBucket
{
    public static string? Of(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade)) return null;
        var s = grade.ToLowerInvariant();
        if (s.Contains("maudu") || s.Contains("fabricat")) return "Maudu";
        if (s.Contains("da'if") || s.Contains("daif") || s.Contains("weak") || s.Contains("munkar")) return "Daif";
        if (s.Contains("sahih") || s.Contains("authentic")) return "Sahih";
        if (s.Contains("hasan") || s.Contains("good")) return "Hasan";
        return "Other";
    }
}
