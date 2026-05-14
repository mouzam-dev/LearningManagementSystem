using LMS.Application.Admin.Dtos;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Queries;

/// <summary>
/// Paginated audit trail, newest-first, optionally filtered by entity type
/// ("User" / "Course") so admins can scope the view.
/// </summary>
public record GetAuditLogQuery(
    string? Entity = null,
    int Page = 1,
    int PageSize = 30
) : IRequest<AdminAuditLogPage>;

public class GetAuditLogQueryHandler
    : IRequestHandler<GetAuditLogQuery, AdminAuditLogPage>
{
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _db;

    public GetAuditLogQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminAuditLogPage> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Entity))
        {
            var entity = request.Entity.Trim();
            query = query.Where(a => a.Entity == entity);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Left-join the acting user so the trail shows a name, not just a Guid.
        var rows = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.Action,
                a.Entity,
                a.EntityId,
                a.Changes,
                a.CreatedAt,
                Actor = _db.Users
                    .Where(u => u.Id == a.UserId)
                    .Select(u => new { u.FirstName, u.LastName, u.Email })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new AdminAuditLogDto
        {
            Id = r.Id,
            ActorId = r.UserId,
            ActorName = r.Actor is null
                ? "System"
                : (r.Actor.FirstName + " " + r.Actor.LastName).Trim(),
            ActorEmail = r.Actor?.Email ?? string.Empty,
            Action = r.Action,
            Entity = r.Entity,
            EntityId = r.EntityId,
            Changes = r.Changes,
            CreatedAt = r.CreatedAt,
        }).ToList();

        return new AdminAuditLogPage
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
