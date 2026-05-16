using LMS.Application.Common;
using LMS.Application.OrgAdmin.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.OrgAdmin.Queries;

/// <summary>
/// Org-scoped course list. Filters strictly on <c>Course.OrganizationId</c> so
/// SuperAdmin-owned (platform-level) courses don't leak into a tenant's view.
/// </summary>
public record GetOrgCoursesQuery(
    string? Search = null,
    string? Category = null,
    bool? IsPublished = null,
    Guid? BranchId = null,
    int Page = 1,
    int PageSize = 25
) : IRequest<OrgAdminCoursesPage>;

public class GetOrgCoursesQueryHandler
    : IRequestHandler<GetOrgCoursesQuery, OrgAdminCoursesPage>
{
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrgCoursesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OrgAdminCoursesPage> Handle(GetOrgCoursesQuery request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _db.Courses
            .AsNoTracking()
            .Where(c => c.OrganizationId == orgId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim();
            query = query.Where(c => c.Category == category);
        }

        if (request.IsPublished is bool published)
        {
            query = query.Where(c => c.IsPublished == published);
        }

        if (request.BranchId is Guid branchId)
        {
            query = query.Where(c => c.BranchId == branchId);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new OrgAdminCourseListItemDto
            {
                CourseId = c.Id,
                Title = c.Title,
                Category = c.Category,
                IsPublished = c.IsPublished,
                TeacherId = c.TeacherId,
                TeacherName = (c.Teacher.FirstName + " " + c.Teacher.LastName).Trim(),
                TeacherEmail = c.Teacher.Email,
                BranchId = c.BranchId,
                BranchName = c.Branch != null ? c.Branch.Name : null,
                ModuleCount = c.Modules.Count,
                LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(),
                StudentCount = c.Enrollments.Count,
                AssessmentCount = c.Assessments.Count,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
            })
            .ToListAsync(ct);

        return new OrgAdminCoursesPage
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
