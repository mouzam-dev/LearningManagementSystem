using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetCourseAnalyticsQuery(Guid CourseId) : IRequest<CourseAnalyticsDto>;

public class GetCourseAnalyticsQueryHandler
    : IRequestHandler<GetCourseAnalyticsQuery, CourseAnalyticsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseAnalyticsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CourseAnalyticsDto> Handle(GetCourseAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId
                && (c.TeacherId == teacherId
                    || c.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        // -----------------------------------------------------------------
        // Headline counts
        // -----------------------------------------------------------------

        var enrolments = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == course.Id)
            .Select(e => new
            {
                e.StudentId,
                e.ProgressPercentage,
                e.IsCompleted,
                StudentName = e.Student.FirstName + " " + e.Student.LastName,
                StudentEmail = e.Student.Email,
            })
            .ToListAsync(cancellationToken);

        var totalStudents = enrolments.Count;
        var completedStudents = enrolments.Count(e => e.IsCompleted);
        var activeStudents = enrolments.Count(e => !e.IsCompleted && e.ProgressPercentage > 0);
        var avgProgress = totalStudents == 0 ? 0m : Math.Round(enrolments.Average(e => e.ProgressPercentage), 1);
        var completionRate = totalStudents == 0 ? 0 : (int)Math.Round(100.0 * completedStudents / totalStudents);

        var totalLessons = await _db.Lessons
            .AsNoTracking()
            .CountAsync(l => l.Module.CourseId == course.Id && l.IsPublished, cancellationToken);
        var totalAssessments = await _db.Assessments
            .AsNoTracking()
            .CountAsync(a => a.CourseId == course.Id, cancellationToken);
        var totalSubmissions = await _db.Submissions
            .AsNoTracking()
            .CountAsync(s => s.Assessment.CourseId == course.Id, cancellationToken);
        var ungradedSubmissions = await _db.Submissions
            .AsNoTracking()
            .CountAsync(s => s.Assessment.CourseId == course.Id && s.Score == null, cancellationToken);
        var certificatesIssued = await _db.Certificates
            .AsNoTracking()
            .CountAsync(c => c.CourseId == course.Id, cancellationToken);

        // -----------------------------------------------------------------
        // Per-lesson completion rate
        // -----------------------------------------------------------------

        var lessonStats = await _db.Lessons
            .AsNoTracking()
            .Where(l => l.Module.CourseId == course.Id && l.IsPublished)
            .OrderBy(l => l.Module.Order).ThenBy(l => l.Order)
            .Select(l => new
            {
                l.Id,
                l.Title,
                ModuleTitle = l.Module.Title,
                ModuleOrder = l.Module.Order,
                LessonOrder = l.Order,
                CompletedCount = _db.LessonProgress.Count(p => p.LessonId == l.Id && p.CompletedAt != null),
            })
            .ToListAsync(cancellationToken);

        var lessonStatsDtos = lessonStats.Select(l => new LessonAnalyticsDto
        {
            LessonId = l.Id,
            LessonTitle = l.Title,
            ModuleTitle = l.ModuleTitle,
            ModuleOrder = l.ModuleOrder,
            LessonOrder = l.LessonOrder,
            CompletedCount = l.CompletedCount,
            CompletionRate = totalStudents == 0 ? 0 : (int)Math.Round(100.0 * l.CompletedCount / totalStudents),
        }).ToList();

        // -----------------------------------------------------------------
        // Per-assessment stats
        // -----------------------------------------------------------------

        var assessmentStats = await _db.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == course.Id)
            .OrderBy(a => a.DueDate ?? DateTime.MaxValue)
            .ThenBy(a => a.Title)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Type,
                a.PassingScore,
                Submissions = a.Submissions.Select(s => new { s.Score }).ToList(),
            })
            .ToListAsync(cancellationToken);

        var assessmentStatsDtos = assessmentStats.Select(a =>
        {
            var graded = a.Submissions.Where(s => s.Score != null).Select(s => s.Score!.Value).ToList();
            var passed = graded.Count(score => score >= a.PassingScore);
            return new AssessmentAnalyticsDto
            {
                AssessmentId = a.Id,
                AssessmentTitle = a.Title,
                AssessmentType = a.Type,
                PassingScore = a.PassingScore,
                SubmissionCount = a.Submissions.Count,
                AverageScore = graded.Count == 0 ? (int?)null : (int)Math.Round(graded.Average()),
                PassRate = graded.Count == 0 ? 0 : (int)Math.Round(100.0 * passed / graded.Count),
                UngradedCount = a.Submissions.Count - graded.Count,
            };
        }).ToList();

        // -----------------------------------------------------------------
        // Top 5 students + at-risk students (low progress + idle 14+ days)
        // -----------------------------------------------------------------

        // Pull last-activity for at-risk computation. Use submission timestamps
        // because seed data has them; in production we'd also pull lesson
        // progress timestamps and take the max.
        var lastActivity = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.Assessment.CourseId == course.Id)
            .GroupBy(s => s.StudentId)
            .Select(g => new { StudentId = g.Key, LastAt = (DateTime?)g.Max(s => s.SubmittedAt) })
            .ToDictionaryAsync(g => g.StudentId, g => g.LastAt, cancellationToken);

        var topStudents = enrolments
            .OrderByDescending(e => e.ProgressPercentage)
            .ThenByDescending(e => e.IsCompleted)
            .Take(5)
            .Select(e => new StudentMiniDto
            {
                StudentId = e.StudentId,
                StudentName = e.StudentName,
                StudentEmail = e.StudentEmail,
                ProgressPercentage = e.ProgressPercentage,
                LastActivityAt = lastActivity.GetValueOrDefault(e.StudentId),
            })
            .ToList();

        var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);
        var atRiskStudents = enrolments
            .Where(e => !e.IsCompleted && e.ProgressPercentage < 50)
            .Select(e => new
            {
                Enrol = e,
                LastAt = lastActivity.GetValueOrDefault(e.StudentId),
            })
            .Where(x => x.LastAt is null || x.LastAt < twoWeeksAgo)
            .OrderBy(x => x.Enrol.ProgressPercentage)
            .ThenBy(x => x.LastAt ?? DateTime.MinValue)
            .Take(5)
            .Select(x => new StudentMiniDto
            {
                StudentId = x.Enrol.StudentId,
                StudentName = x.Enrol.StudentName,
                StudentEmail = x.Enrol.StudentEmail,
                ProgressPercentage = x.Enrol.ProgressPercentage,
                LastActivityAt = x.LastAt,
            })
            .ToList();

        return new CourseAnalyticsDto
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            TotalStudents = totalStudents,
            ActiveStudents = activeStudents,
            CompletedStudents = completedStudents,
            AverageProgress = avgProgress,
            CompletionRate = completionRate,
            CertificatesIssued = certificatesIssued,
            TotalLessons = totalLessons,
            TotalAssessments = totalAssessments,
            TotalSubmissions = totalSubmissions,
            UngradedSubmissions = ungradedSubmissions,
            LessonStats = lessonStatsDtos,
            AssessmentStats = assessmentStatsDtos,
            TopStudents = topStudents,
            AtRiskStudents = atRiskStudents,
        };
    }
}
