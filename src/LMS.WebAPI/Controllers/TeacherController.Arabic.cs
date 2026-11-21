using FluentValidation;
using LMS.Application.Arabic;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>Learn Arabic progress of the students enrolled in the teacher's courses.</summary>
public partial class TeacherController
{
    [HttpGet("arabic-progress")]
    [ProducesResponseType(typeof(TeacherArabicProgressDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeacherArabicProgressDto>> GetArabicProgress(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        try { return Ok(await _mediator.Send(new GetTeacherArabicProgressQuery(search, page, pageSize), ct)); }
        catch (ValidationException ex) { return ValidationProblem(BuildModelState(ex)); }
    }
}
