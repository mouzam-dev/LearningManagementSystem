using FluentValidation;
using LMS.Application.Devices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>Push-notification tokens for the signed-in user's phones (Android app).</summary>
[ApiController]
[Authorize]
[Route("api/devices")]
public class DevicesController(IMediator mediator) : ControllerBase
{
    public record DeviceRequest(string Token, string? Platform);

    [HttpPost]
    public Task<IActionResult> Register([FromBody] DeviceRequest body, CancellationToken ct) =>
        Run(new RegisterDeviceCommand(body.Token, body.Platform), ct);

    [HttpPost("unregister")]
    public Task<IActionResult> Unregister([FromBody] DeviceRequest body, CancellationToken ct) =>
        Run(new UnregisterDeviceCommand(body.Token), ct);

    private async Task<IActionResult> Run(IRequest command, CancellationToken ct)
    {
        try
        {
            await mediator.Send(command, ct);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(new ValidationProblemDetails(ex.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }
    }
}
