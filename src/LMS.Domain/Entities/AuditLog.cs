namespace LMS.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }       // nullable: system actions have no user
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Changes { get; set; }    // JSON before/after
    public DateTime CreatedAt { get; set; }
}
