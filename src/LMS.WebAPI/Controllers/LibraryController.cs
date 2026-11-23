using LMS.Application.Common;
using LMS.WebAPI.Services.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Library search over our own Qur'an and hadith tables (free), plus AI answers and study notes
/// written only from those tables (Claude, capped per user per day). Search is open to everyone
/// (rate limited per address); AI answers need an account.
/// </summary>
[ApiController]
[Authorize]
[Route("api/library")]
public class LibraryController(LibrarySearchService search, LibraryAiService ai, ICurrentUserService user) : ControllerBase
{
    public record AskRequest(string Question, string? Mode);

    [HttpGet("search")]
    [AllowAnonymous]
    [EnableRateLimiting("library-search")]
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

    [HttpGet("usage")]
    public async Task<IActionResult> Usage(CancellationToken ct) =>
        Ok(new { aiEnabled = ai.Enabled, used = await ai.UsedTodayAsync(user.GetUserId(), ct), limit = ai.DailyLimit });

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest body, CancellationToken ct)
    {
        var question = (body.Question ?? "").Trim();
        var mode = body.Mode is "notes" ? "notes" : "answer";
        if (question.Length is < 3 or > 500) return ValidationProblem(Problem("question", "Ask in 3 to 500 characters."));
        try
        {
            return Ok(await ai.AskAsync(user.GetUserId(), question, mode, ct));
        }
        catch (AiLimitReachedException ex)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message + " It resets at midnight (UTC)." });
        }
        catch (AiUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    private static ValidationProblemDetails Problem(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
