namespace LMS.Application.Common;

/// <summary>
/// Creates in-app notifications and (in a later slice) sends matching emails.
/// Centralized here so any command handler can drop a notification without
/// reaching into the DbContext or duplicating fanout logic.
/// </summary>
public interface INotificationService
{
    /// <summary>Create a notification for a single user. Caller is expected to call SaveChanges.</summary>
    Task NotifyAsync(
        Guid userId, string type, string title, string message, CancellationToken ct = default);

    /// <summary>Create a notification for many users in one batch. Caller is expected to call SaveChanges.</summary>
    Task FanOutAsync(
        IEnumerable<Guid> userIds, string type, string title, string message, CancellationToken ct = default);
}
