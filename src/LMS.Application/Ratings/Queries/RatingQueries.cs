using LMS.Application.Common;
using LMS.Application.Ratings.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Ratings.Queries;

// --------------- Course ratings (with summary + caller's own) ---------------

public record GetCourseRatingsQuery(Guid CourseId, int Page = 1, int PageSize = 10)
    : IRequest<CourseRatingsPageDto>;

public class GetCourseRatingsQueryHandler
    : IRequestHandler<GetCourseRatingsQuery, CourseRatingsPageDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseRatingsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CourseRatingsPageDto> Handle(GetCourseRatingsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var baseQuery = _db.CourseRatings.AsNoTracking()
            .Where(r => r.CourseId == request.CourseId);

        // Pull all ratings once and aggregate in-memory. With one rating per
        // (student, course), even popular courses are well under the few-
        // thousand-row range where this remains cheaper than 6 separate counts.
        var all = await baseQuery
            .Select(r => new { r.Rating })
            .ToListAsync(ct);

        var summary = new RatingSummaryDto
        {
            TotalCount = all.Count,
            Average = all.Count == 0 ? 0 : Math.Round(all.Average(x => x.Rating), 2),
            OneStar = all.Count(x => x.Rating == 1),
            TwoStar = all.Count(x => x.Rating == 2),
            ThreeStar = all.Count(x => x.Rating == 3),
            FourStar = all.Count(x => x.Rating == 4),
            FiveStar = all.Count(x => x.Rating == 5),
        };

        var items = await baseQuery
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CourseRatingDto
            {
                Id = r.Id,
                CourseId = r.CourseId,
                StudentId = r.StudentId,
                StudentName = (r.Student.FirstName + " " + r.Student.LastName).Trim(),
                StudentAvatarUrl = r.Student.ProfilePictureUrl,
                Rating = r.Rating,
                Review = r.Review,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            })
            .ToListAsync(ct);

        CourseRatingDto? myRating = null;
        if (_currentUser.IsAuthenticated)
        {
            var me = _currentUser.GetUserId();
            myRating = await baseQuery
                .Where(r => r.StudentId == me)
                .Select(r => new CourseRatingDto
                {
                    Id = r.Id,
                    CourseId = r.CourseId,
                    StudentId = r.StudentId,
                    StudentName = (r.Student.FirstName + " " + r.Student.LastName).Trim(),
                    StudentAvatarUrl = r.Student.ProfilePictureUrl,
                    Rating = r.Rating,
                    Review = r.Review,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                })
                .FirstOrDefaultAsync(ct);
        }

        return new CourseRatingsPageDto
        {
            Summary = summary,
            Items = items,
            MyRating = myRating,
        };
    }
}

// --------------- Teacher ratings (across all that teacher's courses) ---------------

public record GetTeacherRatingsQuery(Guid TeacherId, int Page = 1, int PageSize = 10)
    : IRequest<TeacherRatingsPageDto>;

public class GetTeacherRatingsQueryHandler
    : IRequestHandler<GetTeacherRatingsQuery, TeacherRatingsPageDto>
{
    private readonly IApplicationDbContext _db;

    public GetTeacherRatingsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TeacherRatingsPageDto> Handle(GetTeacherRatingsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var baseQuery = _db.TeacherRatings.AsNoTracking()
            .Where(r => r.TeacherId == request.TeacherId);

        var all = await baseQuery
            .Select(r => new { r.Rating })
            .ToListAsync(ct);

        var summary = new RatingSummaryDto
        {
            TotalCount = all.Count,
            Average = all.Count == 0 ? 0 : Math.Round(all.Average(x => x.Rating), 2),
            OneStar = all.Count(x => x.Rating == 1),
            TwoStar = all.Count(x => x.Rating == 2),
            ThreeStar = all.Count(x => x.Rating == 3),
            FourStar = all.Count(x => x.Rating == 4),
            FiveStar = all.Count(x => x.Rating == 5),
        };

        var items = await baseQuery
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new TeacherRatingDto
            {
                Id = r.Id,
                TeacherId = r.TeacherId,
                StudentId = r.StudentId,
                StudentName = (r.Student.FirstName + " " + r.Student.LastName).Trim(),
                StudentAvatarUrl = r.Student.ProfilePictureUrl,
                CourseId = r.CourseId,
                CourseTitle = r.Course.Title,
                Rating = r.Rating,
                Review = r.Review,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            })
            .ToListAsync(ct);

        return new TeacherRatingsPageDto
        {
            Summary = summary,
            Items = items,
        };
    }
}
