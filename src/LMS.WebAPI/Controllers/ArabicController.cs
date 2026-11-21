using LMS.Application.Arabic;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// "Learn Arabic" course (Al-Arabiyyah Bayna Yadayk, text only). Reading is public like the
/// Qur'an / Hadith readers; saving quiz progress needs a signed-in user.
/// </summary>
[ApiController]
[Route("api/arabic")]
public class ArabicController(IMediator mediator) : ControllerBase
{
    [HttpGet("books")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ArabicBookDto>>> GetBooks(CancellationToken ct)
        => Ok(await mediator.Send(new GetArabicBooksQuery(), ct));

    [HttpGet("units/{unitId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ArabicUnitDto>> GetUnit(string unitId, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new GetArabicUnitQuery(unitId), ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("words")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ArabicWordCardDto>>> GetWords(
        [FromQuery] int book = 1, [FromQuery] string? unitId = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetArabicWordsQuery(book, unitId), ct));

    [HttpGet("progress")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ArabicProgressDto>>> GetProgress(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyArabicProgressQuery(), ct));

    [HttpPost("progress")]
    [Authorize]
    public async Task<ActionResult<ArabicProgressDto>> SaveProgress(SaveArabicProgressCommand command, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(command, ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (FluentValidation.ValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }
    }
}
