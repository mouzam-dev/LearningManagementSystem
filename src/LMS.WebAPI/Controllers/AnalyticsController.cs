using FluentValidation;
using LMS.Application.Analytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LMS.WebAPI.Controllers;

/// <summary>First-party visit tracking (any visitor) and the super-admin Analytics report.</summary>
[ApiController]
[Route("api/analytics")]
public class AnalyticsController(IMediator mediator) : ControllerBase
{
    public record PageViewRequest(Guid VisitorId, string? Path);

    /// <summary>Called by the app on every page change. Always 204: telemetry must never break the page.</summary>
    [HttpPost("pageview")]
    [AllowAnonymous]
    [EnableRateLimiting("pageviews")]
    public async Task<IActionResult> PageView([FromBody] PageViewRequest body, CancellationToken ct)
    {
        await mediator.Send(new RecordPageViewCommand(body.VisitorId, body.Path, Request.Headers.UserAgent.ToString()), ct);
        return NoContent();
    }

    [HttpGet("admin")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<AdminAnalyticsDto>> Report([FromQuery] int days = 30, CancellationToken ct = default)
    {
        try { return Ok(await mediator.Send(new GetAdminAnalyticsQuery(days), ct)); }
        catch (ValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(ex.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }
    }
}
