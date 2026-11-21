using LMS.Application.Arabic;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// "Learn Arabic" course data for the embedded learn-app (/learn-arabic/). Signed-in users only, so each
/// learner's progress is tracked on their account.
/// </summary>
[ApiController]
[Authorize]
[Route("api/arabic")]
public class ArabicController(IMediator mediator) : ControllerBase
{
    /// <summary>The whole course in the embedded learn-app's catalog format (~4 MB JSON, gzip-compressed).</summary>
    [HttpGet("catalog")]
    public async Task<ActionResult<IReadOnlyList<ArabicCatalogBookDto>>> GetCatalog(CancellationToken ct)
        => Ok(await mediator.Send(new GetArabicCatalogQuery(), ct));

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
