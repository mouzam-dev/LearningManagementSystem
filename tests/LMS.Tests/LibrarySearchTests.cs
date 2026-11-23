using LMS.WebAPI.Services.Library;
using Xunit;

namespace LMS.Tests;

public class LibrarySearchTests
{
    [Fact]
    public void English_words_get_translation_synonyms_and_lose_accents()
    {
        var (en, ar) = LibrarySearchService.BuildQueries("How to perform Salah in Ramaḍān", all: true);
        Assert.Equal("how & to & (salah | prayer | salat) & in & ramadan", en); // "perform" is question filler
        Assert.Equal("", ar);
        Assert.Equal("how | to | (salah | prayer | salat) | in | ramadan", LibrarySearchService.BuildQueries("How to perform Salah in Ramaḍān", false).English);
        // a query made only of filler words is still searched as typed
        Assert.Equal("perform", LibrarySearchService.BuildQueries("perform", all: true).English);
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
}
