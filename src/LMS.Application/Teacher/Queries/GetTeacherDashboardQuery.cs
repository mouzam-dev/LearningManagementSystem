using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetTeacherDashboardQuery() : IRequest<TeacherDashboardDto>;

public class GetTeacherDashboardQueryHandler : IRequestHandler<GetTeacherDashboardQuery, TeacherDashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTeacherDashboardQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherDashboardDto> Handle(GetTeacherDashboardQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        // Aggregate over the caller's courses only.
        var ownedCourses = _db.Courses.AsNoTracking().Where(c => c.TeacherId == teacherId);

        var totalCourses = await ownedCourses.CountAsync(cancellationToken);
        var publishedCourses = await ownedCourses.CountAsync(c => c.IsPublished, cancellationToken);

        // "Enrolment" here means rows in Enrollments scoped to this teacher's courses.
        var enrolmentsOfMine = _db.Enrollments
            .AsNoTracking()
            .Where(e => e.Course.TeacherId == teacherId);

        var totalStudents = await enrolmentsOfMine
            .Select(e => e.StudentId)
            .Distinct()
            .CountAsync(cancellationToken);

        // "Active" = at least one lesson started (progress > 0) but not yet complete.
        var totalActiveStudents = await enrolmentsOfMine
            .CountAsync(e => e.ProgressPercentage > 0 && !e.IsCompleted, cancellationToken);

        var completedEnrolments = await enrolmentsOfMine
            .CountAsync(e => e.IsCompleted, cancellationToken);

        var pendingGradingCount = await _db.Submissions
            .AsNoTracking()
            .CountAsync(s => s.Assessment.Course.TeacherId == teacherId
                          && s.Score == null,
                cancellationToken);

        var certificatesIssued = await _db.Certificates
            .AsNoTracking()
            .CountAsync(c => c.Course.TeacherId == teacherId, cancellationToken);

        var topCourses = await ownedCourses
            .OrderByDescending(c => c.Enrollments.Count)
            .ThenByDescending(c => c.UpdatedAt)
            .Take(5)
            .Select(c => new TeacherCourseSummary
            {
                CourseId = c.Id,
                Title = c.Title,
                Category = c.Category,
                IsPublished = c.IsPublished,
                StudentCount = c.Enrollments.Count,
                LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(l => l.IsPublished),
                UpdatedAt = c.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        var recentEnrollments = await enrolmentsOfMine
            .OrderByDescending(e => e.EnrolledAt)
            .Take(8)
            .Select(e => new RecentEnrollmentDto
            {
                EnrollmentId = e.Id,
                CourseId = e.CourseId,
                CourseTitle = e.Course.Title,
                StudentName = e.Student.FirstName + " " + e.Student.LastName,
                EnrolledAt = e.EnrolledAt,
            })
            .ToListAsync(cancellationToken);

        var pendingGrading = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.Assessment.Course.TeacherId == teacherId && s.Score == null)
            .OrderBy(s => s.SubmittedAt)
            .Take(8)
            .Select(s => new PendingGradingDto
            {
                SubmissionId = s.Id,
                AssessmentId = s.AssessmentId,
                AssessmentTitle = s.Assessment.Title,
                CourseTitle = s.Assessment.Course.Title,
                StudentName = s.Student.FirstName + " " + s.Student.LastName,
                SubmittedAt = s.SubmittedAt,
            })
            .ToListAsync(cancellationToken);

        return new TeacherDashboardDto
        {
            Summary = new TeacherDashboardSummary
            {
                TotalCourses = totalCourses,
                PublishedCourses = publishedCourses,
                DraftCourses = totalCourses - publishedCourses,
                TotalStudents = totalStudents,
                TotalActiveStudents = totalActiveStudents,
                CompletedEnrollments = completedEnrolments,
                PendingGradingCount = pendingGradingCount,
                CertificatesIssued = certificatesIssued,
            },
            TopCourses = topCourses,
            RecentEnrollments = recentEnrollments,
            PendingGrading = pendingGrading,
        };
    }
}
