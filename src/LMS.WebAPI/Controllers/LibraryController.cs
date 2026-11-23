using LMS.WebAPI.Services.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Library search and topic notes over our own Qur'an and hadith tables. Free, open to everyone
/// (rate limited per address); nothing is sent to any outside service.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("library-search")]
[Route("api/library")]
public class LibraryController(LibrarySearchService search) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<LibraryResults>> Search([FromQuery] string q, [FromQuery] string scope = "all",
        [FromQuery] string? collection = null, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        q = (q ?? "").Trim();
        if (q.Length is < 2 or > 200) return ValidationProblem(Problem("q", "Type 2 to 200 characters to search."));
        if (scope is not ("all" or "quran" or "hadith")) return ValidationProblem(Problem("scope", "Scope must be all, quran or hadith."));
        page = Math.Clamp(page, 1, 50);
        const int size = 20;
        return Ok(await search.SearchAsync(q, scope, collection, size, size, (page - 1) * size, ct));
    }

    /// <summary>Notes on a topic or question: matching verses, then hadith grouped by chapter.</summary>
    [HttpGet("notes")]
    public async Task<ActionResult<LibraryNotes>> Notes([FromQuery] string q, CancellationToken ct = default)
    {
        q = (q ?? "").Trim();
        if (q.Length is < 2 or > 200) return ValidationProblem(Problem("q", "Type 2 to 200 characters."));
        return Ok(await search.NotesAsync(q, ct));
    }

    private static ValidationProblemDetails Problem(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
