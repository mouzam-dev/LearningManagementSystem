using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

public record GetCourseDetailQuery(Guid CourseId) : IRequest<CourseDetailDto>;

public class GetCourseDetailQueryHandler
    : IRequestHandler<GetCourseDetailQuery, CourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseDetailQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CourseDetailDto> Handle(GetCourseDetailQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var course = await _db.Courses
            .AsNoTracking()
            .Include(c => c.Teacher)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.IsPublished, cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        var enrollment = await _db.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CourseId == course.Id && e.StudentId == userId, cancellationToken);

        if (enrollment is null)
        {
            throw new UnauthorizedAccessException("You must enrol in this course before viewing its content.");
        }

        var modules = await _db.Modules
            .AsNoTracking()
            .Where(m => m.CourseId == course.Id)
            .OrderBy(m => m.Order)
            .Select(m => new ModuleDto
            {
                ModuleId = m.Id,
                Title = m.Title,
                Description = m.Description,
                Order = m.Order,
                Lessons = m.Lessons
                    .Where(l => l.IsPublished)
                    .OrderBy(l => l.Order)
                    .Select(l => new LessonListItemDto
                    {
                        LessonId = l.Id,
                        Title = l.Title,
                        Type = l.Type,
                        Duration = l.Duration,
                        Order = l.Order,
                        IsCompleted = l.Progress.Any(p => p.UserId == userId && p.CompletedAt != null),
                        WatchTimeSeconds = l.Progress
                            .Where(p => p.UserId == userId)
                            .Select(p => (int?)p.WatchTimeSeconds)
                            .FirstOrDefault() ?? 0,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var totalLessons = modules.Sum(m => m.Lessons.Count);
        var completedLessons = modules.Sum(m => m.Lessons.Count(l => l.IsCompleted));
        var enrolledCount = await _db.Enrollments.CountAsync(e => e.CourseId == course.Id, cancellationToken);

        // Continue-learning target: first incomplete lesson in module/lesson order.
        Guid? nextLessonId = null;
        foreach (var m in modules)
        {
            var first = m.Lessons.FirstOrDefault(l => !l.IsCompleted);
            if (first is not null) { nextLessonId = first.LessonId; break; }
        }
        // If everything is done, fall back to the very first lesson so the user
        // can revisit. If there are no lessons at all, leave it null.
        nextLessonId ??= modules.SelectMany(m => m.Lessons).FirstOrDefault()?.LessonId;

        return new CourseDetailDto
        {
            CourseId = course.Id,
            Title = course.Title,
            Description = course.Description,
            Category = course.Category,
            ThumbnailUrl = course.ThumbnailUrl,
            TeacherName = (course.Teacher.FirstName + " " + course.Teacher.LastName).Trim(),
            EnrolledCount = enrolledCount,
            MaxStudents = course.MaxStudents,
            IsEnrolled = true,
            ProgressPercentage = enrollment.ProgressPercentage,
            TotalLessons = totalLessons,
            CompletedLessons = completedLessons,
            Modules = modules,
            NextLessonId = nextLessonId,
        };
    }
}
