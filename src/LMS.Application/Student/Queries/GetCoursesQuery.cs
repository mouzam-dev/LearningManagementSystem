using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

public record GetCoursesQuery(
    string? Search = null,
    string? Category = null,
    int Page = 1,
    int PageSize = 12
) : IRequest<PagedResult<CourseListItemDto>>;

public class GetCoursesQueryHandler
    : IRequestHandler<GetCoursesQuery, PagedResult<CourseListItemDto>>
{
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCoursesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<CourseListItemDto>> Handle(
        GetCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 12 : request.PageSize, 1, MaxPageSize);

        // Catalog only shows live, non-archived courses. Archived ones are
        // a soft-hidden teacher concept; they shouldn't appear to new students.
        var query = _db.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished && !c.IsArchived);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search.Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.Title, term)
                                  || EF.Functions.Like(c.Description, term));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim();
            query = query.Where(c => c.Category == category);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseListItemDto
            {
                CourseId = c.Id,
                Title = c.Title,
                Description = c.Description,
                Category = c.Category,
                ThumbnailUrl = c.ThumbnailUrl,
                TeacherName = (c.Teacher.FirstName + " " + c.Teacher.LastName).Trim(),
                EnrolledCount = c.Enrollments.Count,
                MaxStudents = c.MaxStudents,
                IsEnrolled = c.Enrollments.Any(e => e.StudentId == userId),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CourseListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
