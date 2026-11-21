using LMS.Application.Arabic;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// "Learn Arabic" course (Al-Arabiyyah Bayna Yadayk, text only). Signed-in users only, so each
/// learner's progress is tracked on their account.
/// </summary>
[ApiController]
[Authorize]
[Route("api/arabic")]
public class ArabicController(IMediator mediator) : ControllerBase
{
    [HttpGet("books")]
    public async Task<ActionResult<IReadOnlyList<ArabicBookDto>>> GetBooks(CancellationToken ct)
        => Ok(await mediator.Send(new GetArabicBooksQuery(), ct));

    [HttpGet("units/{unitId}")]
    public async Task<ActionResult<ArabicUnitDto>> GetUnit(string unitId, CancellationToken ct)
    {
        try { return Ok(await mediator.Send(new GetArabicUnitQuery(unitId), ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("words")]
    public async Task<ActionResult<IReadOnlyList<ArabicWordCardDto>>> GetWords(
        [FromQuery] int book = 1, [FromQuery] string? unitId = null, CancellationToken ct = default)
        => Ok(await mediator.Send(new GetArabicWordsQuery(book, unitId), ct));

    [HttpGet("progress")]
    public async Task<ActionResult<IReadOnlyList<ArabicProgressDto>>> GetProgress(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyArabicProgressQuery(), ct));

    [HttpPost("progress")]
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
