using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetCourseStudentsQuery(Guid CourseId)
    : IRequest<IReadOnlyList<CourseStudentListItemDto>>;

public class GetCourseStudentsQueryHandler
    : IRequestHandler<GetCourseStudentsQuery, IReadOnlyList<CourseStudentListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseStudentsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CourseStudentListItemDto>> Handle(
        GetCourseStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");
        if (course.TeacherId != teacherId)
        {
            throw new KeyNotFoundException($"Course {request.CourseId} was not found.");
        }

        var totalLessons = await _db.Lessons
            .AsNoTracking()
            .CountAsync(l => l.Module.CourseId == course.Id && l.IsPublished, cancellationToken);

        // Base roster — one row per enrolment with the cheap fields the DB
        // already has on Enrollment.
        var roster = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == course.Id)
            .OrderBy(e => e.IsCompleted)               // active enrolments first
            .ThenByDescending(e => e.ProgressPercentage)
            .ThenBy(e => e.EnrolledAt)
            .Select(e => new
            {
                e.StudentId,
                StudentName = e.Student.FirstName + " " + e.Student.LastName,
                e.Student.Email,
                e.EnrolledAt,
                e.ProgressPercentage,
                e.IsCompleted,
                e.CompletedAt,
            })
            .ToListAsync(cancellationToken);

        if (roster.Count == 0)
        {
            return Array.Empty<CourseStudentListItemDto>();
        }

        var studentIds = roster.Select(r => r.StudentId).ToList();

        // Lesson-completion counts — one query, grouped by student.
        var completionMap = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => studentIds.Contains(p.UserId)
                     && p.CompletedAt != null
                     && p.Lesson.Module.CourseId == course.Id
                     && p.Lesson.IsPublished)
            .GroupBy(p => p.UserId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.StudentId, g => g.Count, cancellationToken);

        // Most-recent lesson-activity per student.
        var lessonActivityMap = await _db.LessonProgress
            .AsNoTracking()
            .Where(p => studentIds.Contains(p.UserId)
                     && p.Lesson.Module.CourseId == course.Id)
            .GroupBy(p => p.UserId)
            .Select(g => new { StudentId = g.Key, LastAt = (DateTime?)g.Max(p => p.CompletedAt) })
            .ToDictionaryAsync(g => g.StudentId, g => g.LastAt, cancellationToken);

        // Submission count + most recent submission + avg score per student.
        var submissionMap = await _db.Submissions
            .AsNoTracking()
            .Where(s => studentIds.Contains(s.StudentId) && s.Assessment.CourseId == course.Id)
            .GroupBy(s => s.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                Count = g.Count(),
                LastAt = (DateTime?)g.Max(s => s.SubmittedAt),
                AvgScore = g.Where(s => s.Score != null).Average(s => (double?)s.Score),
            })
            .ToDictionaryAsync(g => g.StudentId, g => g, cancellationToken);

        // Certificate presence.
        var certStudentIds = await _db.Certificates
            .AsNoTracking()
            .Where(c => c.CourseId == course.Id && studentIds.Contains(c.UserId))
            .Select(c => c.UserId)
            .ToListAsync(cancellationToken);
        var certSet = certStudentIds.ToHashSet();

        return roster.Select(r =>
        {
            var lessonsCompleted = completionMap.GetValueOrDefault(r.StudentId, 0);
            var lessonLast = lessonActivityMap.GetValueOrDefault(r.StudentId);
            submissionMap.TryGetValue(r.StudentId, out var sub);
            var subCount = sub?.Count ?? 0;
            var subLast = sub?.LastAt;
            var avgScore = sub?.AvgScore is double a ? (int?)Math.Round(a) : null;

            return new CourseStudentListItemDto
            {
                StudentId = r.StudentId,
                StudentName = r.StudentName,
                StudentEmail = r.Email,
                EnrolledAt = r.EnrolledAt,
                ProgressPercentage = r.ProgressPercentage,
                IsCompleted = r.IsCompleted,
                CompletedAt = r.CompletedAt,
                LessonsCompleted = lessonsCompleted,
                TotalLessons = totalLessons,
                SubmissionsCount = subCount,
                AverageScore = avgScore,
                LastActivityAt = MaxOrNull(lessonLast, subLast),
                HasCertificate = certSet.Contains(r.StudentId),
            };
        }).ToList();
    }

    private static DateTime? MaxOrNull(DateTime? a, DateTime? b) =>
        a is null ? b : (b is null ? a : (a > b ? a : b));
}
