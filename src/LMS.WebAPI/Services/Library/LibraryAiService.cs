using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using LMS.Domain.Entities;
using LMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebAPI.Services.Library;

public record AiPoint(string Text, IReadOnlyList<string> Sources);
public record AiSection(string Heading, IReadOnlyList<AiPoint> Points);
public record AiAnswer(bool Found, string Title, string Summary, IReadOnlyList<AiSection> Sections, string Caution);

/// <summary>A passage the AI was given, with the id its citations use ("Q2:183", "H12345").</summary>
public record AiPassage(string Id, string Kind, string Reference, string TextEn, string TextAr, string? Grade);

public record AiResult(string Mode, string Question, AiAnswer Answer, IReadOnlyList<AiPassage> Passages, int UsedToday, int DailyLimit);

public class AiLimitReachedException(int limit) : Exception($"Daily limit of {limit} AI requests reached.");
public class AiUnavailableException(string message) : Exception(message);

/// <summary>
/// AI answers and notes written ONLY from our own Qur'an and hadith tables:
/// 1) Claude turns the question into keyword searches, 2) <see cref="LibrarySearchService"/> runs them
/// against the database, 3) Claude organizes the passages it was given, citing each by id.
/// Citations to anything it was not given are dropped. Needs Anthropic:ApiKey; capped per user per day.
/// </summary>
public class LibraryAiService(
    ApplicationDbContext db,
    LibrarySearchService search,
    IConfiguration config,
    ILogger<LibraryAiService> log)
{
    private const string Model = "claude-haiku-5-5";
    private const int MaxVerses = 15, MaxHadith = 25;

    public bool Enabled => !string.IsNullOrWhiteSpace(config["Anthropic:ApiKey"]);
    public int DailyLimit => config.GetValue("Library:AiDailyLimit", 20);

    public Task<int> UsedTodayAsync(Guid userId, CancellationToken ct) =>
        db.AiQueryLogs.CountAsync(l => l.UserId == userId && l.CreatedAt >= DateTime.UtcNow.Date, ct);

    public async Task<AiResult> AskAsync(Guid userId, string question, string mode, CancellationToken ct)
    {
        if (!Enabled) throw new AiUnavailableException("AI answers are not switched on yet.");
        var used = await UsedTodayAsync(userId, ct);
        if (used >= DailyLimit) throw new AiLimitReachedException(DailyLimit);

        var client = new AnthropicClient { ApiKey = config["Anthropic:ApiKey"] };
        var entry = new AiQueryLog { UserId = userId, Mode = mode, Question = question.Length > 500 ? question[..500] : question, CreatedAt = DateTime.UtcNow };
        db.AiQueryLogs.Add(entry); // counted even if it fails below: the API call may already have cost money
        try
        {
            // 1) keyword searches in the vocabulary of the translations
            var plan = await CallJsonAsync(client, PlanPrompt, question, PlanSchema, Effort.Low, 2000, entry, ct);
            var queries = plan.GetProperty("queries").EnumerateArray().Select(q => q.GetString() ?? "")
                .Where(q => q.Trim().Length > 0).Take(6).Prepend(question).Distinct().ToList();

            // 2) our database only
            var passages = await GatherAsync(queries, ct);
            entry.PassageCount = passages.Count;
            AiAnswer answer;
            if (passages.Count == 0)
            {
                answer = new AiAnswer(false, question, "Nothing in our Qur'an and hadith library matched this question.", [], "");
            }
            else
            {
                // 3) organize, citing passage ids
                var json = await CallJsonAsync(client, mode == "notes" ? NotesPrompt : AnswerPrompt,
                    BuildUserMessage(question, passages), AnswerSchema, Effort.Medium, 16000, entry, ct);
                answer = ParseAnswer(json, passages.Select(p => p.Id).ToHashSet());
            }
            entry.Succeeded = true;
            await db.SaveChangesAsync(CancellationToken.None);

            var cited = answer.Sections.SelectMany(s => s.Points).SelectMany(p => p.Sources).ToHashSet();
            var shown = cited.Count > 0 ? passages.Where(p => cited.Contains(p.Id)).ToList() : passages;
            return new AiResult(mode, question, answer, shown, used + 1, DailyLimit);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await db.SaveChangesAsync(CancellationToken.None);
            log.LogWarning(ex, "[library-ai] {Mode} failed", mode);
            throw ex switch
            {
                AiUnavailableException => ex,
                AnthropicRateLimitException => new AiUnavailableException("The AI service is busy. Please try again in a minute."),
                AnthropicUnauthorizedException => new AiUnavailableException("The AI service key is not valid. Ask the admin to check it."),
                AnthropicApiException or AnthropicIOException => new AiUnavailableException("The AI service could not be reached. Please try again."),
                _ => new AiUnavailableException("Something went wrong preparing the answer. Please try again."),
            };
        }
    }

    private async Task<List<AiPassage>> GatherAsync(List<string> queries, CancellationToken ct)
    {
        var verses = new List<List<QuranHit>>();
        var hadith = new List<List<HadithHit>>();
        foreach (var q in queries)
        {
            var r = await search.SearchAsync(q, "all", null, 8, 12, 0, ct);
            verses.Add([.. r.Quran]);
            hadith.Add([.. r.Hadith]);
        }
        // Round-robin across the queries so every angle of the question is represented.
        var outV = RoundRobin(verses, v => $"Q{v.Surah}:{v.Ayah}", MaxVerses)
            .Select(v => new AiPassage($"Q{v.Surah}:{v.Ayah}", "quran", $"Qur'an {v.Surah}:{v.Ayah} ({v.SurahName})", v.TextEn, v.TextAr, null));
        var outH = RoundRobin(hadith, h => $"H{h.Id}", MaxHadith)
            .Select(h => new AiPassage($"H{h.Id}", "hadith", $"{h.CollectionTitle} {h.HadithNumber}" + (string.IsNullOrEmpty(h.BookName) ? "" : $", {h.BookName}"),
                h.TextEn, h.TextAr, h.Grade));
        return [.. outV, .. outH];
    }

    private static List<T> RoundRobin<T>(List<List<T>> lists, Func<T, string> key, int max)
    {
        var seen = new HashSet<string>();
        var result = new List<T>();
        for (var i = 0; result.Count < max && lists.Any(l => i < l.Count); i++)
            foreach (var l in lists)
                if (i < l.Count && result.Count < max && seen.Add(key(l[i]))) result.Add(l[i]);
        return result;
    }

    private static string BuildUserMessage(string question, List<AiPassage> passages)
    {
        var sb = new StringBuilder("<passages>\n");
        foreach (var p in passages)
        {
            sb.Append($"<passage id=\"{p.Id}\" source=\"{p.Reference}\"");
            if (!string.IsNullOrEmpty(p.Grade)) sb.Append($" grade=\"{p.Grade}\"");
            sb.Append(">\n").Append(p.TextEn).Append("\n</passage>\n");
        }
        return sb.Append("</passages>\n\n<question>\n").Append(question).Append("\n</question>").ToString();
    }

    private static async Task<JsonElement> CallJsonAsync(AnthropicClient client, string system, string user,
        Dictionary<string, JsonElement> schema, Effort effort, int maxTokens, AiQueryLog entry, CancellationToken ct)
    {
        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = Model,
            MaxTokens = maxTokens,
            System = system,
            OutputConfig = new OutputConfig { Effort = effort, Format = new JsonOutputFormat { Schema = schema } },
            Messages = [new UserMessageParam { Content = user }],
        }, ct);
        entry.InputTokens += (int)response.Usage.InputTokens;
        entry.OutputTokens += (int)response.Usage.OutputTokens;

        if (response.StopReason == "refusal") throw new AiUnavailableException("The AI declined to answer this question.");
        if (response.StopReason == "max_tokens") throw new AiUnavailableException("The answer was too long to finish. Try a narrower question.");
        var text = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    /// <summary>Reads the structured answer and drops any citation that is not one of the passages we sent.</summary>
    public static AiAnswer ParseAnswer(JsonElement json, IReadOnlySet<string> known)
    {
        var sections = json.GetProperty("sections").EnumerateArray().Select(s => new AiSection(
                s.GetProperty("heading").GetString() ?? "",
                s.GetProperty("points").EnumerateArray().Select(p => new AiPoint(
                        p.GetProperty("text").GetString() ?? "",
                        p.GetProperty("sources").EnumerateArray().Select(x => x.GetString() ?? "").Where(known.Contains).Distinct().ToList()))
                    .Where(p => p.Sources.Count > 0) // a point with no valid source is not from our library
                    .ToList()))
            .Where(s => s.Points.Count > 0)
            .ToList();
        var found = json.GetProperty("found").GetBoolean() && sections.Count > 0;
        return new AiAnswer(found, json.GetProperty("title").GetString() ?? "", json.GetProperty("summary").GetString() ?? "",
            sections, json.GetProperty("caution").GetString() ?? "");
    }

    // ---- prompts & schemas ------------------------------------------------------------------

    private const string PlanPrompt = """
        You help search a library that contains only the Qur'an (Saheeh International English and Arabic) and hadith
        collections (Sunnah.com English translations and Arabic). Turn the user's question or topic into 3 to 6 short
        keyword searches (1-4 words each) that will find the relevant verses and hadith with a full-text search.
        Use the vocabulary these English translations actually use - for example "prayer" for salah/namaz,
        "ablution" for wudu, "fasting" for sawm/roza, "pilgrimage" for hajj, "supplication" for dua - and add an
        Arabic keyword search when it helps. If the question is in Urdu or another language, still write the
        searches in English (and Arabic). Do not answer the question.
        """;

    private const string Rules = """
        Rules you must follow:
        - Use ONLY the passages provided in <passages>. They come from this site's own Qur'an and hadith library.
          Do not add anything from outside knowledge, other books, scholars, websites or your own opinion - even
          well-known facts - unless a provided passage states it.
        - Every point must list in "sources" the ids (like "Q2:183" or "H1234") of the passages it is based on.
          Never cite an id that is not in <passages>. If a point is not supported by a passage, leave it out.
        - If the passages do not answer the question, set "found" to false, give no sections, and say in "summary"
          what the library did not cover.
        - Mention a hadith's grade when the passage gives one and it is weak (da'if) or fabricated.
        - Do not give fatwas or rulings beyond what the passages say. Put any limits - such as passages that only
          partly cover the topic or details that need a scholar - in "caution" (empty string if none).
        - Write in the language of the question (English, Urdu or Arabic). Keep wording close to the passages.
        """;

    private const string AnswerPrompt = """
        You are a careful research assistant for an Islamic learning site. Answer the user's question by organizing
        what the provided passages say: a short direct "summary", then sections with clear headings and points (for
        a "how to" question, put the steps in order).
        """ + "\n" + Rules;

    private const string NotesPrompt = """
        You are a careful research assistant for an Islamic learning site. Prepare well-organized study notes on the
        user's topic from the provided passages: an introductory "summary", then sections with headings (for example
        meaning and importance, how it is done, virtues, warnings, related etiquette - only those the passages
        support), each with concise points a student can revise from.
        """ + "\n" + Rules;

    private static Dictionary<string, JsonElement> Schema(object o) =>
        JsonSerializer.SerializeToElement(o).EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());

    private static readonly Dictionary<string, JsonElement> PlanSchema = Schema(new
    {
        type = "object",
        properties = new { queries = new { type = "array", items = new { type = "string" } } },
        required = new[] { "queries" },
        additionalProperties = false,
    });

    private static readonly Dictionary<string, JsonElement> AnswerSchema = Schema(new
    {
        type = "object",
        properties = new
        {
            found = new { type = "boolean" },
            title = new { type = "string" },
            summary = new { type = "string" },
            sections = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        heading = new { type = "string" },
                        points = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    text = new { type = "string" },
                                    sources = new { type = "array", items = new { type = "string" } },
                                },
                                required = new[] { "text", "sources" },
                                additionalProperties = false,
                            },
                        },
                    },
                    required = new[] { "heading", "points" },
                    additionalProperties = false,
                },
            },
            caution = new { type = "string" },
        },
        required = new[] { "found", "title", "summary", "sections", "caution" },
        additionalProperties = false,
    });
}
