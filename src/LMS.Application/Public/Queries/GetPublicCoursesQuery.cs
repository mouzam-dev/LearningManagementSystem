using LMS.Application.Common;
using LMS.Application.Public.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Public.Queries;

/// <summary>
/// Paginated list of published, non-archived courses for anonymous visitors
/// on the marketing homepage. No user context required.
/// </summary>
public record GetPublicCoursesQuery(
    string? Search = null,
    string? Category = null,
    Guid? TeacherId = null,
    int Page = 1,
    int PageSize = 12
) : IRequest<PagedResult<PublicCourseListItemDto>>;

public class GetPublicCoursesQueryHandler
    : IRequestHandler<GetPublicCoursesQuery, PagedResult<PublicCourseListItemDto>>
{
    private const int MaxPageSize = 50;

    private readonly IApplicationDbContext _db;

    public GetPublicCoursesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PublicCourseListItemDto>> Handle(
        GetPublicCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 12 : request.PageSize, 1, MaxPageSize);

        var query = _db.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished && !c.IsArchived);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search.Trim().ToLower()}%";
            query = query.Where(c => EF.Functions.Like(c.Title.ToLower(), term)
                                  || EF.Functions.Like(c.Description.ToLower(), term));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var category = request.Category.Trim();
            query = query.Where(c => c.Category == category);
        }

        if (request.TeacherId is { } teacherId && teacherId != Guid.Empty)
        {
            query = query.Where(c => c.TeacherId == teacherId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.Enrollments.Count)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new PublicCourseListItemDto
            {
                CourseId = c.Id,
                Title = c.Title,
                Description = c.Description,
                Category = c.Category,
                ThumbnailUrl = c.ThumbnailUrl,
                TeacherName = (c.Teacher.FirstName + " " + c.Teacher.LastName).Trim(),
                EnrolledCount = c.Enrollments.Count,
                MaxStudents = c.MaxStudents,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PublicCourseListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }
}
