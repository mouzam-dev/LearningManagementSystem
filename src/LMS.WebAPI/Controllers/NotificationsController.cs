using LMS.Application.Notifications.Commands;
using LMS.Application.Notifications.Dtos;
using LMS.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// In-app notification feed for the signed-in user. Open to any authenticated
/// role — Students, Teachers, OrgAdmin, and SuperAdmin all receive notifications.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) { _mediator = mediator; }

    [HttpGet]
    [ProducesResponseType(typeof(NotificationFeedDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationFeedDto>> Get(
        [FromQuery] int limit = 30,
        [FromQuery] bool unreadOnly = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetMyNotificationsQuery(limit, unreadOnly), ct);
        return Ok(result);
    }

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken ct)
    {
        var ok = await _mediator.Send(new MarkNotificationReadCommand(notificationId), ct);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> MarkAllRead(CancellationToken ct)
    {
        var marked = await _mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return Ok(new { marked });
    }
}
