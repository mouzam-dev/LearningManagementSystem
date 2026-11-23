using System.Text.Json;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Loads the full Qur'an (Data/quran.json: Uthmani Arabic, Saheeh International English,
/// Jalandhari Urdu; built once from the quran.com API) into QuranSurahs/QuranVerses.
/// Runs only while QuranSurahs is empty, so it is safe on every startup.
/// </summary>
public static class QuranSeeder
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.QuranSurahs.AnyAsync(ct))
            return;

        var file = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seeding", "Data", "quran.json");
        if (!File.Exists(file))
        {
            logger.LogWarning("QuranSeeder: {File} not found, skipping.", file);
            return;
        }

        var data = JsonSerializer.Deserialize<QuranJson>(await File.ReadAllTextAsync(file, ct), Json)!;
        var surahs = data.Surahs.Select(s => new QuranSurah
        {
            Number = s.N,
            NameAr = s.NameAr,
            NameEn = s.NameEn,
            NameTransliterated = s.NameTr,
            RevelationPlace = s.Place,
            VerseCount = s.Verses.Count,
            Verses = s.Verses.Select(v => new QuranVerse
            {
                SurahNumber = s.N, VerseNumber = v.N, TextAr = v.Ar, TextEn = v.En, TextUr = v.Ur,
            }).ToList(),
        }).ToList();

        db.QuranSurahs.AddRange(surahs);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("QuranSeeder: imported {Surahs} surahs, {Verses} verses.",
            surahs.Count, surahs.Sum(s => s.VerseCount));
    }

    private record QuranJson(List<SurahJson> Surahs);
    private record SurahJson(int N, string NameAr, string NameEn, string NameTr, string Place, List<VerseJson> Verses);
    private record VerseJson(int N, string Ar, string En, string Ur);
}
