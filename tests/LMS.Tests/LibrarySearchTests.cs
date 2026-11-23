using System.Text.Json;
using LMS.WebAPI.Services.Library;
using Xunit;

namespace LMS.Tests;

public class LibrarySearchTests
{
    [Fact]
    public void English_words_get_translation_synonyms_and_lose_accents()
    {
        var (en, ar) = LibrarySearchService.BuildQueries("How to perform Salah in Ramaḍān", all: true);
        Assert.Equal("how & to & perform & (salah | prayer | salat) & in & ramadan", en);
        Assert.Equal("", ar);
        Assert.Equal("how | to | perform | (salah | prayer | salat) | in | ramadan", LibrarySearchService.BuildQueries("How to perform Salah in Ramaḍān", false).English);
    }

    [Fact]
    public void Arabic_words_are_folded_like_the_database_index()
    {
        // Uthmani الصلوٰة (waw + dagger alif) and the plain spelling must give the same term.
        Assert.Equal(LibrarySearchService.NormalizeArabic("الصلاة"), LibrarySearchService.NormalizeArabic("ٱلصَّلَوٰةَ"));
        var (en, ar) = LibrarySearchService.BuildQueries("إِيمَان نماز", all: true);
        Assert.Equal("", en);
        Assert.Equal("ايمان & نماز", ar);
    }

    [Fact]
    public void Query_text_cannot_inject_tsquery_operators()
    {
        var (en, _) = LibrarySearchService.BuildQueries("prayer') | !:* & <-> fasting", all: true);
        Assert.Equal("prayer & fasting", en);
    }

    [Fact]
    public void Markup_and_html_are_stripped()
    {
        Assert.Equal("Narrated Abu Huraira: The Prophet said", LibrarySearchService.StripMarkup("<p>[narrator id=\"1\"]Narrated Abu Huraira[/narrator]: [matn]The Prophet said[/matn]</p>"));
    }

    [Fact]
    public void Ai_answer_keeps_only_citations_to_passages_it_was_given()
    {
        var json = JsonDocument.Parse("""
            {"found": true, "title": "Prayer", "summary": "s", "caution": "",
             "sections": [
               {"heading": "Steps", "points": [
                 {"text": "Wash first", "sources": ["Q5:6", "Q99:1"]},
                 {"text": "Invented point", "sources": ["H999"]}]},
               {"heading": "Only invented", "points": [{"text": "x", "sources": []}]}]}
            """).RootElement;

        var answer = LibraryAiService.ParseAnswer(json, new HashSet<string> { "Q5:6", "H1" });

        Assert.True(answer.Found);
        var section = Assert.Single(answer.Sections);
        var point = Assert.Single(section.Points);
        Assert.Equal("Wash first", point.Text);
        Assert.Equal(["Q5:6"], point.Sources);
    }

    [Fact]
    public void Ai_answer_with_no_supported_points_is_not_found()
    {
        var json = JsonDocument.Parse("""
            {"found": true, "title": "t", "summary": "s", "caution": "",
             "sections": [{"heading": "h", "points": [{"text": "x", "sources": ["Q1:99"]}]}]}
            """).RootElement;

        Assert.False(LibraryAiService.ParseAnswer(json, new HashSet<string> { "Q1:1" }).Found);
    }
}
