using System.Text.Json;
using LMS.Domain.Entities;

namespace LMS.Application.Admin;

/// <summary>
/// Helper for building <see cref="AuditLog"/> rows from admin command handlers.
/// Keeps the audit-write boilerplate out of each handler — they just call
/// <c>_db.AuditLogs.Add(AdminAudit.Entry(...))</c> before SaveChanges.
/// </summary>
public static class AdminAudit
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public static AuditLog Entry(
        Guid actorId,
        string action,
        string entity,
        Guid entityId,
        object? changes = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = actorId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Changes = changes is null ? null : JsonSerializer.Serialize(changes, JsonOpts),
            CreatedAt = DateTime.UtcNow,
        };
    }
}
