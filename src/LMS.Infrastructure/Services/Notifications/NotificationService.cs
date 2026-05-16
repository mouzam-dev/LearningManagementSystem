using LMS.Application.Common;
using LMS.Domain.Entities;

namespace LMS.Infrastructure.Services.Notifications;

/// <summary>
/// Default implementation: writes Notification rows. Save is left to the
/// caller's unit-of-work so a notification + its triggering command commit
/// atomically. Email transport is fire-and-forget alongside the in-app row.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;

    public NotificationService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task NotifyAsync(Guid userId, string type, string title, string message, CancellationToken ct = default)
    {
        _db.Notifications.Add(Build(userId, type, title, message));
        return Task.CompletedTask;
    }

    public Task FanOutAsync(IEnumerable<Guid> userIds, string type, string title, string message, CancellationToken ct = default)
    {
        foreach (var id in userIds.Distinct())
        {
            _db.Notifications.Add(Build(id, type, title, message));
        }
        return Task.CompletedTask;
    }

    private static Notification Build(Guid userId, string type, string title, string message) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = type,
        Title = title,
        Message = message,
        IsRead = false,
        CreatedAt = DateTime.UtcNow,
    };
}
