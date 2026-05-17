using LMS.Application.Announcements.Dtos;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Announcements.Queries;

// ---------------- Teacher: announcements for a course I own ----------------

public record GetCourseAnnouncementsQuery(Guid CourseId)
    : IRequest<IReadOnlyList<AnnouncementDto>>;

public class GetCourseAnnouncementsQueryHandler
    : IRequestHandler<GetCourseAnnouncementsQuery, IReadOnlyList<AnnouncementDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseAnnouncementsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AnnouncementDto>> Handle(
        GetCourseAnnouncementsQuery request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        // Teacher (primary OR co-instructor) must own the course, OR the caller
        // must be a Student enrolled in it. We let students reach this endpoint
        // so the course-detail page can show the announcement timeline without
        // a separate query.
        var course = await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.CourseId)
            .Select(c => new
            {
                c.Id,
                IsTeacherOnCourse = c.TeacherId == teacherId
                    || c.CoInstructors.Any(ci => ci.UserId == teacherId),
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Course not found.");

        if (!course.IsTeacherOnCourse)
        {
            var enrolled = await _db.Enrollments
                .AsNoTracking()
                .AnyAsync(e => e.CourseId == course.Id && e.StudentId == teacherId, ct);
            if (!enrolled)
            {
                throw new UnauthorizedAccessException("You don't have access to this course's announcements.");
            }
        }

        return await _db.Announcements
            .AsNoTracking()
            .Where(a => a.CourseId == request.CourseId)
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto
            {
                Id = a.Id,
                CourseId = a.CourseId,
                CourseTitle = a.Course.Title,
                AuthorId = a.AuthorId,
                AuthorName = (a.Author.FirstName + " " + a.Author.LastName).Trim(),
                Title = a.Title,
                Body = a.Body,
                IsPinned = a.IsPinned,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
            })
            .ToListAsync(ct);
    }
}

// ---------------- Student: recent announcements across enrolled courses ----

public record GetMyAnnouncementsQuery(int Limit = 20)
    : IRequest<IReadOnlyList<AnnouncementDto>>;

public class GetMyAnnouncementsQueryHandler
    : IRequestHandler<GetMyAnnouncementsQuery, IReadOnlyList<AnnouncementDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyAnnouncementsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AnnouncementDto>> Handle(
        GetMyAnnouncementsQuery request, CancellationToken ct)
    {
        var studentId = _currentUser.GetUserId();
        var take = Math.Clamp(request.Limit, 1, 50);

        var enrolledCourseIds = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => e.CourseId)
            .ToListAsync(ct);

        if (enrolledCourseIds.Count == 0)
        {
            return Array.Empty<AnnouncementDto>();
        }

        return await _db.Announcements
            .AsNoTracking()
            .Where(a => enrolledCourseIds.Contains(a.CourseId))
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .Select(a => new AnnouncementDto
            {
                Id = a.Id,
                CourseId = a.CourseId,
                CourseTitle = a.Course.Title,
                AuthorId = a.AuthorId,
                AuthorName = (a.Author.FirstName + " " + a.Author.LastName).Trim(),
                Title = a.Title,
                Body = a.Body,
                IsPinned = a.IsPinned,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
            })
            .ToListAsync(ct);
    }
}
