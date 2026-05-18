using LMS.Application.Common;
using LMS.Application.Public.Dtos;
using LMS.Application.Public.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Anonymous endpoints that power the marketing/landing page — public course
/// listing and category chips. Everything else (enrollment, course detail with
/// lessons, etc.) stays on the role-scoped controllers.
/// </summary>
[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("courses")]
    [ProducesResponseType(typeof(PagedResult<PublicCourseListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PublicCourseListItemDto>>> GetCourses(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetPublicCoursesQuery(search, category, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken ct)
    {
        var categories = await _mediator.Send(new GetPublicCategoriesQuery(), ct);
        return Ok(categories);
    }
}
