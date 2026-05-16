namespace LMS.Application.Notifications.Dtos;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationFeedDto
{
    public int UnreadCount { get; set; }
    public IReadOnlyList<NotificationDto> Items { get; set; } = Array.Empty<NotificationDto>();
}
