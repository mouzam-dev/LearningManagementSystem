using LMS.Application.Admin.Dtos;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Queries;

/// <summary>
/// Platform-wide course list for admins — every course from every teacher,
/// with optional search (title), category, and published-status filtering.
/// </summary>
public record GetAdminCoursesQuery(
    string? Search = null,
    string? Category = null,
    bool? IsPublished = null,
    int Page = 1,
    int PageSize = 25
) : IRequest<AdminCoursesPage>;

public class GetAdminCoursesQueryHandler
    : IRequestHandler<GetAdminCoursesQuery, AdminCoursesPage>
{
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _db;

    public GetAdminCoursesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminCoursesPage> Handle(GetAdminCoursesQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _db.Courses.AsNoTracking().AsQueryable();

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

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminCourseListItemDto
            {
                CourseId = c.Id,
                Title = c.Title,
                Category = c.Category,
                IsPublished = c.IsPublished,
                TeacherId = c.TeacherId,
                TeacherName = (c.Teacher.FirstName + " " + c.Teacher.LastName).Trim(),
                TeacherEmail = c.Teacher.Email,
                ModuleCount = c.Modules.Count,
                LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(),
                StudentCount = c.Enrollments.Count,
                AssessmentCount = c.Assessments.Count,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new AdminCoursesPage
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
