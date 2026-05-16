using LMS.Application.Common;
using LMS.Application.OrgAdmin.Dtos;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.OrgAdmin.Queries;

public record GetOrgAdminDashboardQuery() : IRequest<OrgAdminDashboardDto>;

public class GetOrgAdminDashboardQueryHandler
    : IRequestHandler<GetOrgAdminDashboardQuery, OrgAdminDashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrgAdminDashboardQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OrgAdminDashboardDto> Handle(
        GetOrgAdminDashboardQuery request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var org = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.Id == orgId)
            .Select(o => new { o.Id, o.Name, o.Slug, o.Description })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException(
                $"Organization {orgId} (from your token) was not found.");

        var teacherCount = await _db.Users
            .CountAsync(u => u.OrganizationId == orgId && u.Role == Roles.Teacher, ct);
        var studentCount = await _db.Users
            .CountAsync(u => u.OrganizationId == orgId && u.Role == Roles.Student, ct);

        // Courses carry a denormalized OrganizationId so we can scope without
        // joining through Users.
        var courseQuery = _db.Courses.Where(c => c.OrganizationId == orgId);
        var courseCount = await courseQuery.CountAsync(ct);
        var publishedCount = await courseQuery.CountAsync(c => c.IsPublished, ct);

        var branches = await _db.Branches
            .AsNoTracking()
            .Where(b => b.OrganizationId == orgId)
            .OrderBy(b => b.Name)
            .Select(b => new OrgAdminBranchSummary
            {
                Id = b.Id,
                Name = b.Name,
                Code = b.Code,
                IsActive = b.IsActive,
                TeacherCount = b.Users.Count(u => u.Role == Roles.Teacher),
                StudentCount = b.Users.Count(u => u.Role == Roles.Student),
            })
            .ToListAsync(ct);

        return new OrgAdminDashboardDto
        {
            OrganizationId = org.Id,
            OrganizationName = org.Name,
            OrganizationSlug = org.Slug,
            OrganizationDescription = org.Description,
            BranchCount = branches.Count,
            TeacherCount = teacherCount,
            StudentCount = studentCount,
            CourseCount = courseCount,
            PublishedCourseCount = publishedCount,
            Branches = branches,
        };
    }
}
