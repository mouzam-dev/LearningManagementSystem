using LMS.Application.Common;
using LMS.Application.Notifications.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Notifications.Queries;

public record GetMyNotificationsQuery(int Limit = 30, bool UnreadOnly = false)
    : IRequest<NotificationFeedDto>;

public class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, NotificationFeedDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<NotificationFeedDto> Handle(GetMyNotificationsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.GetUserId();
        var take = Math.Clamp(request.Limit, 1, 100);

        var baseQ = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        var unreadCount = await baseQ.CountAsync(n => !n.IsRead, ct);

        var listQ = request.UnreadOnly ? baseQ.Where(n => !n.IsRead) : baseQ;
        var items = await listQ
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync(ct);

        return new NotificationFeedDto { UnreadCount = unreadCount, Items = items };
    }
}
