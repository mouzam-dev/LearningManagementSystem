namespace LMS.Domain.Entities;

/// <summary>
/// One page visit in the app, for the admin Analytics page. First-party only: no IP address and no
/// third-party tracker; a visitor is a random id the browser keeps, linked to the user when signed in.
/// </summary>
public class PageView
{
    public long Id { get; set; }
    public Guid VisitorId { get; set; }
    public Guid? UserId { get; set; }
    public string? Role { get; set; }
    /// <summary>Normalised route, ids replaced: "/teacher/courses/:id/students".</summary>
    public string Path { get; set; } = string.Empty;
    /// <summary>"mobile", "tablet" or "desktop", from the browser's user agent.</summary>
    public string Device { get; set; } = "desktop";
    public DateTime CreatedAt { get; set; }
}
