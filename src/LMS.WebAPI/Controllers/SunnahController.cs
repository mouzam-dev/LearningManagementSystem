using LMS.WebAPI.Services.Sunnah;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Thin proxy to the Sunnah.com API (collections -> books -> hadiths). The API key
/// lives server-side; the SPA only ever talks to this controller. Any authenticated
/// user may read.
/// </summary>
[ApiController]
[Route("api/sunnah")]
[Authorize]
public class SunnahController : ControllerBase
{
    private readonly ISunnahService _sunnah;
    private readonly ILogger<SunnahController> _logger;

    public SunnahController(ISunnahService sunnah, ILogger<SunnahController> logger)
    {
        _sunnah = sunnah;
        _logger = logger;
    }

    [HttpGet("collections")]
    [ProducesResponseType(typeof(SunnahPageDto<SunnahCollectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Collections([FromQuery] int page = 1, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        try { return Ok(await _sunnah.GetCollectionsAsync(page, Math.Clamp(limit, 1, 100), ct)); }
        catch (Exception ex) { return Upstream(ex); }
    }

    [HttpGet("collections/{collection}/books")]
    [ProducesResponseType(typeof(SunnahPageDto<SunnahBookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Books(string collection, [FromQuery] int page = 1, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        try { return Ok(await _sunnah.GetBooksAsync(collection, page, Math.Clamp(limit, 1, 200), ct)); }
        catch (Exception ex) { return Upstream(ex); }
    }

    [HttpGet("collections/{collection}/books/{bookNumber}/hadiths")]
    [ProducesResponseType(typeof(SunnahPageDto<SunnahHadithDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Hadiths(string collection, string bookNumber, [FromQuery] int page = 1, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        try { return Ok(await _sunnah.GetHadithsAsync(collection, bookNumber, page, Math.Clamp(limit, 1, 100), ct)); }
        catch (Exception ex) { return Upstream(ex); }
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(SunnahPageDto<SunnahHadithDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] string? collection,
        [FromQuery] string? grade,
        [FromQuery] string? book,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 25,
        CancellationToken ct = default)
    {
        try { return Ok(await _sunnah.SearchAsync(q, collection, grade, book, page, Math.Clamp(limit, 1, 50), ct)); }
        catch (Exception ex) { return Upstream(ex); }
    }

    [HttpGet("random")]
    [ProducesResponseType(typeof(SunnahHadithDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Random(CancellationToken ct = default)
    {
        try { return Ok(await _sunnah.GetRandomAsync(ct)); }
        catch (Exception ex) { return Upstream(ex); }
    }

    private IActionResult Upstream(Exception ex)
    {
        _logger.LogWarning(ex, "Sunnah.com API request failed");
        return StatusCode(StatusCodes.Status502BadGateway, new { message = "The hadith service is unavailable right now." });
    }
}
