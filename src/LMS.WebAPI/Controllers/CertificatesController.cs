using LMS.Application.Student.Dtos;
using LMS.Application.Student.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Public certificate endpoints. Lives outside <see cref="StudentController"/>
/// because verification must work anonymously (anyone with a verify code can
/// confirm the cert is real without an account).
/// </summary>
[ApiController]
[Route("api/certificates")]
[AllowAnonymous]
public class CertificatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CertificatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("verify/{code}")]
    [ProducesResponseType(typeof(VerifyCertificateDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VerifyCertificateDto>> Verify(string code, CancellationToken ct)
    {
        var result = await _mediator.Send(new VerifyCertificateQuery(code), ct);
        // Always 200 — the body's IsValid flag tells the caller whether the
        // code matched a real certificate. Returning 404 for unknown codes
        // would let an attacker enumerate valid prefixes by timing 404 vs 200.
        return Ok(result);
    }
}
