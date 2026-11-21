using System.Text.Json;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Loads the "Al-Arabiyyah Bayna Yadayk" course text (Data/arabic/bNuNN.json, one file per unit)
/// into the Arabic* tables. Runs only while ArabicUnits is empty, so it is safe on every startup.
/// Images and audio from the original app are intentionally not imported.
/// </summary>
public static class ArabicContentSeeder
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.ArabicUnits.AnyAsync(ct))
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

        db.ArabicUnits.AddRange(units);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("ArabicContentSeeder: imported {Units} units, {Lessons} lessons.",
            units.Count, units.Sum(u => u.Lessons.Count));
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
                Order = i, Speaker = d.Who ?? "", Ar = d.Ar ?? "", Tr = d.Tr ?? "", En = d.En ?? "", Ur = d.Ur ?? "",
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
    public record Line(string? Who, string? Ar, string? Tr, string? En, string? Ur);
    public record Quiz(Text Q, List<string> Options, List<string>? Options_Ar, int Answer);
    public record LessonJson(int N, Text Title, Text? Intro, List<Line>? Dialogue, List<Line>? Vocab, List<Quiz>? Quiz);
    public record UnitJson(string Id, int Book, int Unit, Text Title, List<LessonJson> Lessons);
}
