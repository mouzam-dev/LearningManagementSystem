namespace LMS.Domain.Entities;

/// <summary>A phone that receives push notifications for a user (Firebase Cloud Messaging token).</summary>
public class DeviceToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Platform { get; set; } = "android";
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    public User User { get; set; } = null!;
}
