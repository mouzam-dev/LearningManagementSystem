using LMS.Application.Common;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.SuperAdmin.Queries;

public record GetOrganizationsQuery(string? Search, int Page = 1, int PageSize = 25)
    : IRequest<PagedResult<OrganizationListItemDto>>;

public class GetOrganizationsQueryHandler
    : IRequestHandler<GetOrganizationsQuery, PagedResult<OrganizationListItemDto>>
{
    private readonly IApplicationDbContext _db;

    public GetOrganizationsQueryHandler(IApplicationDbContext db) { _db = db; }

    public async Task<PagedResult<OrganizationListItemDto>> Handle(
        GetOrganizationsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var q = _db.Organizations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(o => o.Name.Contains(s) || (o.Slug != null && o.Slug.Contains(s)));
        }

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrganizationListItemDto
            {
                Id = o.Id,
                Name = o.Name,
                Slug = o.Slug,
                ContactEmail = o.ContactEmail,
                IsActive = o.IsActive,
                BranchCount = o.Branches.Count,
                UserCount = o.Users.Count,
                OrgAdminCount = o.Users.Count(u => u.Role == Roles.OrgAdmin),
                TeacherCount = o.Users.Count(u => u.Role == Roles.Teacher),
                StudentCount = o.Users.Count(u => u.Role == Roles.Student),
                CreatedAt = o.CreatedAt,
            })
            .ToListAsync(ct);

        return new PagedResult<OrganizationListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}
