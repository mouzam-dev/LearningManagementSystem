using LMS.Application.Common;
using LMS.Application.OrgAdmin.Dtos;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.OrgAdmin.Queries;

public record GetOrgTeachersQuery(string? Search, Guid? BranchId)
    : IRequest<IReadOnlyList<OrgTeacherDto>>;

public class GetOrgTeachersQueryHandler
    : IRequestHandler<GetOrgTeachersQuery, IReadOnlyList<OrgTeacherDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrgTeachersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<OrgTeacherDto>> Handle(
        GetOrgTeachersQuery request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var q = _db.Users.AsNoTracking()
            .Where(u => u.OrganizationId == orgId && u.Role == Roles.Teacher);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            q = q.Where(u => u.FirstName.Contains(s) || u.LastName.Contains(s) || u.Email.Contains(s));
        }
        if (request.BranchId is not null)
        {
            q = q.Where(u => u.BranchId == request.BranchId);
        }

        return await q
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new OrgTeacherDto
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                IsActive = u.IsActive,
                BranchId = u.BranchId,
                BranchName = u.Branch != null ? u.Branch.Name : null,
                CourseCount = _db.Courses.Count(c => c.TeacherId == u.Id),
                PublishedCourseCount = _db.Courses.Count(c => c.TeacherId == u.Id && c.IsPublished),
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync(ct);
    }
}
