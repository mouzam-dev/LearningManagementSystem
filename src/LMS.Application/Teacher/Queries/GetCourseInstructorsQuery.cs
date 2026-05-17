using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

/// <summary>
/// Lists all instructors on a course — the primary teacher first, then any
/// co-instructors sorted by when they were added. The caller must teach the
/// course (primary or co), otherwise we surface a 404.
/// </summary>
public record GetCourseInstructorsQuery(Guid CourseId)
    : IRequest<IReadOnlyList<CourseInstructorDto>>;

public class GetCourseInstructorsQueryHandler
    : IRequestHandler<GetCourseInstructorsQuery, IReadOnlyList<CourseInstructorDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseInstructorsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CourseInstructorDto>> Handle(
        GetCourseInstructorsQuery request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        var course = await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.CourseId
                && (c.TeacherId == teacherId
                    || c.CoInstructors.Any(ci => ci.UserId == teacherId)))
            .Select(c => new { c.Id, c.TeacherId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        var primary = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == course.TeacherId)
            .Select(u => new CourseInstructorDto
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                ProfilePictureUrl = u.ProfilePictureUrl,
                BranchName = u.Branch != null ? u.Branch.Name : null,
                IsPrimary = true,
                AddedAt = null,
            })
            .FirstOrDefaultAsync(ct);

        var cos = await _db.CourseCoInstructors
            .AsNoTracking()
            .Where(ci => ci.CourseId == request.CourseId)
            .OrderBy(ci => ci.AddedAt)
            .Select(ci => new CourseInstructorDto
            {
                UserId = ci.UserId,
                FirstName = ci.User.FirstName,
                LastName = ci.User.LastName,
                Email = ci.User.Email,
                ProfilePictureUrl = ci.User.ProfilePictureUrl,
                BranchName = ci.User.Branch != null ? ci.User.Branch.Name : null,
                IsPrimary = false,
                AddedAt = ci.AddedAt,
            })
            .ToListAsync(ct);

        return primary is null ? cos : new[] { primary }.Concat(cos).ToList();
    }
}
