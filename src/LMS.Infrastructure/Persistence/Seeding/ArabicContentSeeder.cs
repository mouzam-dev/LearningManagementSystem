using System.Text.Json;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Loads the "Al-Arabiyyah Bayna Yadayk" course text (Data/arabic/bNuNN.json, one file per unit)
/// into the Arabic* tables. Imports every unit that is missing or has no lessons, so it is safe on
/// every startup. To ship new content: replace the JSON files and add a migration that runs
/// DELETE FROM "ArabicLessons" - units (and so learners' progress, which hangs off units) are kept,
/// their titles refreshed, and the lessons re-imported on the next boot.
/// Images and audio from the original app are intentionally not imported.
/// </summary>
public static class ArabicContentSeeder
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var existing = await db.ArabicUnits.Select(u => new { u.Id, HasLessons = u.Lessons.Any() }).ToListAsync(ct);
        if (existing.Count > 0 && existing.All(u => u.HasLessons))
            return;

        var dir = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seeding", "Data", "arabic");
        if (!Directory.Exists(dir))
        {
            logger.LogWarning("ArabicContentSeeder: {Dir} not found, skipping.", dir);
            return;
        }

        var units = Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => ToEntity(JsonSerializer.Deserialize<UnitJson>(File.ReadAllText(f), Json)!, logger))
            .ToList();
        var known = existing.ToDictionary(u => u.Id, u => u.HasLessons);

        var imported = 0;
        foreach (var unit in units)
        {
            if (!known.TryGetValue(unit.Id, out var hasLessons))
            {
                db.ArabicUnits.Add(unit);
            }
            else if (!hasLessons)
            {
                var row = await db.ArabicUnits.SingleAsync(x => x.Id == unit.Id, ct);
                (row.TitleAr, row.TitleEn, row.TitleUr) = (unit.TitleAr, unit.TitleEn, unit.TitleUr);
                foreach (var lesson in unit.Lessons) { lesson.UnitId = unit.Id; db.ArabicLessons.Add(lesson); }
            }
            else continue;
            imported++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("ArabicContentSeeder: imported {Units} units.", imported);
    }

    public static ArabicUnit ToEntity(UnitJson u, ILogger logger) => new()
    {
        Id = u.Id,
        BookNumber = u.Book,
        UnitNumber = u.Unit,
        TitleAr = u.Title.Ar ?? "",
        TitleEn = u.Title.En ?? "",
        TitleUr = u.Title.Ur ?? "",
        Lessons = u.Lessons.Select(l => new ArabicLesson
        {
            Number = l.N,
            TitleAr = l.Title.Ar ?? "",
            TitleEn = l.Title.En ?? "",
            TitleUr = l.Title.Ur ?? "",
            IntroAr = l.Intro?.Ar ?? "",
            IntroEn = l.Intro?.En ?? "",
            IntroUr = l.Intro?.Ur ?? "",
            DialogueLines = (l.Dialogue ?? []).Select((d, i) => new ArabicDialogueLine
            {
                Order = i, Speaker = d.Who ?? "", Ar = d.Ar ?? "", Tr = d.Tr ?? "", En = d.En ?? "", Ur = d.Ur ?? "", Source = d.Src,
            }).ToList(),
            VocabItems = (l.Vocab ?? []).Select((v, i) => new ArabicVocabItem
            {
                Order = i, Ar = v.Ar ?? "", Tr = v.Tr ?? "", En = v.En ?? "", Ur = v.Ur ?? "",
            }).ToList(),
            QuizQuestions = (l.Quiz ?? [])
                .Where(q =>
                {
                    // Same checks the original build.py enforced; a bad row is skipped, not fatal.
                    var ok = q.Answer >= 0 && q.Answer < q.Options.Count
                        && q.Options.Distinct().Count() == q.Options.Count
                        && (q.Options_Ar is null || q.Options_Ar.Count == q.Options.Count);
                    if (!ok) logger.LogWarning("ArabicContentSeeder: skipped invalid question in {Unit} lesson {Lesson}.", u.Id, l.N);
                    return ok;
                })
                .Select((q, i) => new ArabicQuizQuestion
                {
                    Order = i,
                    QuestionAr = q.Q.Ar ?? "",
                    QuestionEn = q.Q.En ?? "",
                    QuestionUr = q.Q.Ur ?? "",
                    Options = q.Options,
                    OptionsAr = q.Options_Ar,
                    AnswerIndex = q.Answer,
                }).ToList(),
        }).ToList(),
    };

    // Shapes of the source JSON files.
    public record Text(string? Ar, string? En, string? Ur);
    public record Line(string? Who, string? Ar, string? Tr, string? En, string? Ur, string? Src);
    public record Quiz(Text Q, List<string> Options, List<string>? Options_Ar, int Answer);
    public record LessonJson(int N, Text Title, Text? Intro, List<Line>? Dialogue, List<Line>? Vocab, List<Quiz>? Quiz);
    public record UnitJson(string Id, int Book, int Unit, Text Title, List<LessonJson> Lessons);
}
