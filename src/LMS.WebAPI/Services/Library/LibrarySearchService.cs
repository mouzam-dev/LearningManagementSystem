using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebAPI.Services.Library;

public record QuranHit(int Surah, int Ayah, string SurahName, string SurahNameAr, string TextAr, string TextEn, string TextUr, string Snippet);

public record HadithHit(int Id, string Collection, string CollectionTitle, string BookNumber, string? BookName,
    string HadithNumber, string? Chapter, string? Grade, string TextEn, string TextAr, string Snippet);

public record LibraryResults(string Query, bool MatchedAllWords, int QuranTotal, int HadithTotal,
    IReadOnlyList<QuranHit> Quran, IReadOnlyList<HadithHit> Hadith);

/// <summary>
/// Full-text search over our own Qur'an (QuranVerses) and hadith (Hadiths) tables, using the
/// PostgreSQL "SearchVector" columns from the AddLibrarySearch migration. English words go through
/// the english stemmer, Arabic-script words through lib_norm_ar + the arabic stemmer (Urdu: simple).
/// Every word must match first; if nothing does, any word may match.
/// </summary>
public class LibrarySearchService(ApplicationDbContext db)
{
    /// <summary>Snippet highlight markers (private-use characters; the page turns them into &lt;mark&gt;).</summary>
    public const char MarkStart = '', MarkEnd = '';

    private static readonly Regex Word = new(@"[\p{L}\p{M}\p{N}]+", RegexOptions.Compiled);
    private static readonly Regex ArabicScript = new(@"[؀-ۿ]", RegexOptions.Compiled);
    private static readonly Regex Harakat = new(@"[ً-ٰٟۖ-ۭـ]", RegexOptions.Compiled);
    private static readonly Regex Tag = new(@"\[/?[a-zA-Z][^\]]*\]|<[^>]+>", RegexOptions.Compiled);

    /// <summary>
    /// Transliterated terms people type, mapped to the words the English translations use.
    /// shortcut: a small hand list; extend it when users report searches that find nothing.
    /// </summary>
    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["salah"] = ["prayer", "salat"], ["salat"] = ["prayer", "salah"], ["namaz"] = ["prayer"], ["namaaz"] = ["prayer"],
        ["wudu"] = ["ablution"], ["wudhu"] = ["ablution"], ["wuzu"] = ["ablution"], ["ghusl"] = ["bath"],
        ["sawm"] = ["fasting", "fast"], ["siyam"] = ["fasting"], ["roza"] = ["fasting"], ["rozah"] = ["fasting"],
        ["zakat"] = ["zakah", "alms"], ["zakah"] = ["zakat", "alms"], ["sadaqah"] = ["charity", "alms"], ["sadaqa"] = ["charity"],
        ["hajj"] = ["pilgrimage"], ["umrah"] = ["umra"], ["dua"] = ["supplication"], ["duaa"] = ["supplication"],
        ["jannah"] = ["paradise"], ["jahannam"] = ["hell"], ["nikah"] = ["marriage"], ["talaq"] = ["divorce"],
        ["jumuah"] = ["friday"], ["jummah"] = ["friday"], ["qiyamah"] = ["resurrection"], ["tawbah"] = ["repentance"],
        ["kaaba"] = ["kabah"], ["kaba"] = ["kabah"], ["kaabah"] = ["kabah"], ["sabr"] = ["patience"], ["shirk"] = ["polytheism", "partners"], ["iman"] = ["faith", "belief"], ["imaan"] = ["faith"],
        ["adhan"] = ["call"], ["azan"] = ["call"], ["masjid"] = ["mosque"], ["ruku"] = ["bowing", "bow"], ["sujood"] = ["prostration"],
        ["sajdah"] = ["prostration"], ["riba"] = ["usury", "interest"], ["janazah"] = ["funeral"], ["halal"] = ["lawful"], ["haram"] = ["unlawful", "forbidden"],
    };

    public static string NormalizeArabic(string s) =>
        Harakat.Replace(s.Replace("وٰ", "ا"), "")
            .Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا').Replace('ٱ', 'ا')
            .Replace('ى', 'ي').Replace('ة', 'ه').Replace('ی', 'ي').Replace('ک', 'ك').Replace('ہ', 'ه').Replace('ۃ', 'ه');

    /// <summary>Same folding as lib_fold_en in SQL: Ramaḍān -> ramadan, Kaʿbah -> kabah.</summary>
    public static string FoldLatin(string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) is not (UnicodeCategory.NonSpacingMark or UnicodeCategory.ModifierLetter))
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    // [narrator]/[matn] tags vanish; HTML tags (<p>, <br>) become a space between paragraphs.
    public static string StripMarkup(string? s) => s is null ? "" :
        Regex.Replace(Tag.Replace(s, m => m.Value[0] == '<' ? " " : ""), @"[ 	]{2,}", " ").Trim();

    /// <summary>to_tsquery text for the English words and the Arabic-script words (letters/digits only, so it is safe to embed).</summary>
    public static (string English, string Arabic) BuildQueries(string query, bool all)
    {
        var join = all ? " & " : " | ";
        var en = new List<string>();
        var ar = new List<string>();
        foreach (Match m in Word.Matches(query))
        {
            var w = m.Value;
            if (ArabicScript.IsMatch(w))
            {
                var n = NormalizeArabic(w);
                if (n.Length > 0) ar.Add(n);
            }
            else if (FoldLatin(w) is var f && Synonyms.TryGetValue(f, out var syn))
                en.Add("(" + string.Join(" | ", syn.Prepend(f)) + ")");
            else if (f.Length > 0)
                en.Add(f);
        }
        return (string.Join(join, en.Distinct()), string.Join(join, ar.Distinct()));
    }

    public async Task<LibraryResults> SearchAsync(string query, string scope, string? collection, int quranTake, int hadithTake,
        int skip, CancellationToken ct)
    {
        var all = BuildQueries(query, all: true);
        if (all.English.Length == 0 && all.Arabic.Length == 0)
            return new LibraryResults(query, true, 0, 0, [], []);

        var res = await RunAsync(all, scope, collection, quranTake, hadithTake, skip, ct);
        if (res.QuranTotal + res.HadithTotal > 0) return res with { Query = query, MatchedAllWords = true };
        var any = BuildQueries(query, all: false);
        if (any == all) return res with { Query = query, MatchedAllWords = true };
        return (await RunAsync(any, scope, collection, quranTake, hadithTake, skip, ct)) with { Query = query, MatchedAllWords = false };
    }

    private async Task<LibraryResults> RunAsync((string English, string Arabic) q, string scope, string? collection,
        int quranTake, int hadithTake, int skip, CancellationToken ct)
    {
        var quran = new List<QuranHit>();
        var hadith = new List<HadithHit>();
        int quranTotal = 0, hadithTotal = 0;
        var en = q.English;
        var ar = q.Arabic;
        // Verses are short: highlight the whole verse. Hadith: up to two fragments around the matches.
        var verseOpts = $"HighlightAll=true, StartSel={MarkStart}, StopSel={MarkEnd}";
        var opts = $"MaxFragments=2, MaxWords=40, MinWords=12, FragmentDelimiter=\" … \", StartSel={MarkStart}, StopSel={MarkEnd}";

        // Shared tsquery: english for Latin words, arabic (stemmed) + simple (Urdu) for Arabic-script words.
        // Values are bound as parameters by SqlQuery's interpolation.
        if (scope is "all" or "quran" && quranTake > 0)
        {
            var rows = await db.Database.SqlQuery<QuranRow>($"""
                WITH x AS (SELECT to_tsquery('english', {en}) || to_tsquery('arabic', {ar}) || to_tsquery('simple', {ar}) AS q)
                SELECT v."SurahNumber" AS "Surah", v."VerseNumber" AS "Ayah", s."NameTransliterated" AS "SurahName", s."NameAr" AS "SurahNameAr",
                       v."TextAr", v."TextEn", v."TextUr",
                       CASE WHEN {en} <> '' THEN ts_headline('english', lib_fold_en(v."TextEn"), to_tsquery('english', {en}), {verseOpts}) ELSE v."TextEn" END AS "Snippet",
                       count(*) OVER () AS "Total"
                FROM "QuranVerses" v JOIN "QuranSurahs" s ON s."Number" = v."SurahNumber", x
                WHERE v."SearchVector" @@ x.q
                ORDER BY ts_rank_cd(v."SearchVector", x.q) DESC, v."SurahNumber", v."VerseNumber"
                LIMIT {quranTake} OFFSET {skip}
                """).ToListAsync(ct);
            quranTotal = rows.Count > 0 ? (int)rows[0].Total : 0;
            quran = rows.Select(r => new QuranHit(r.Surah, r.Ayah, r.SurahName, r.SurahNameAr, r.TextAr, r.TextEn, r.TextUr, r.Snippet)).ToList();
        }

        if (scope is "all" or "hadith" && hadithTake > 0)
        {
            var coll = string.IsNullOrWhiteSpace(collection) ? null : collection.Trim().ToLowerInvariant();
            var rows = await db.Database.SqlQuery<HadithRow>($"""
                WITH x AS (SELECT to_tsquery('english', {en}) || to_tsquery('arabic', {ar}) AS q)
                SELECT h."Id", h."Collection", coalesce(c."TitleEn", h."Collection") AS "CollectionTitle", h."BookNumber", h."BookNameEn" AS "BookName",
                       h."HadithNumber", h."ChapterEn" AS "Chapter", h."GradeEn" AS "Grade", h."BodyEn", h."BodyAr",
                       CASE WHEN {en} <> '' THEN ts_headline('english', lib_fold_en(regexp_replace(coalesce(h."BodyEn", ''), '\[/?[a-zA-Z][^\]]*\]|<[^>]+>', ' ', 'g')), to_tsquery('english', {en}), {opts}) ELSE '' END AS "Snippet",
                       count(*) OVER () AS "Total"
                FROM "Hadiths" h LEFT JOIN "HadithCollections" c ON c."Slug" = h."Collection", x
                WHERE h."SearchVector" @@ x.q AND ({coll}::text IS NULL OR h."Collection" = {coll})
                ORDER BY ts_rank_cd(h."SearchVector", x.q) DESC, h."Id"
                LIMIT {hadithTake} OFFSET {skip}
                """).ToListAsync(ct);
            hadithTotal = rows.Count > 0 ? (int)rows[0].Total : 0;
            hadith = rows.Select(r =>
            {
                var bodyEn = StripMarkup(r.BodyEn);
                return new HadithHit(r.Id, r.Collection, r.CollectionTitle, r.BookNumber, r.BookName, r.HadithNumber,
                    StripMarkup(r.Chapter), r.Grade, bodyEn, StripMarkup(r.BodyAr),
                    string.IsNullOrEmpty(r.Snippet) ? Shorten(bodyEn, 300) : r.Snippet);
            }).ToList();
        }

        return new LibraryResults("", true, quranTotal, hadithTotal, quran, hadith);
    }

    private static string Shorten(string s, int max) => s.Length <= max ? s : s[..max].TrimEnd() + "…";

    private sealed class QuranRow
    {
        public int Surah { get; set; }
        public int Ayah { get; set; }
        public string SurahName { get; set; } = "";
        public string SurahNameAr { get; set; } = "";
        public string TextAr { get; set; } = "";
        public string TextEn { get; set; } = "";
        public string TextUr { get; set; } = "";
        public string Snippet { get; set; } = "";
        public long Total { get; set; }
    }

    private sealed class HadithRow
    {
        public int Id { get; set; }
        public string Collection { get; set; } = "";
        public string CollectionTitle { get; set; } = "";
        public string BookNumber { get; set; } = "";
        public string? BookName { get; set; }
        public string HadithNumber { get; set; } = "";
        public string? Chapter { get; set; }
        public string? Grade { get; set; }
        public string? BodyEn { get; set; }
        public string? BodyAr { get; set; }
        public string Snippet { get; set; } = "";
        public long Total { get; set; }
    }
}
