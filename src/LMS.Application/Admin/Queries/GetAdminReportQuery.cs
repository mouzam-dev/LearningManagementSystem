using System.Globalization;
using LMS.Application.Admin.Dtos;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Queries;

public record GetAdminReportQuery() : IRequest<AdminReportDto>;

public class GetAdminReportQueryHandler : IRequestHandler<GetAdminReportQuery, AdminReportDto>
{
    private const int TrendMonths = 6;

    private readonly IApplicationDbContext _db;

    public GetAdminReportQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminReportDto> Handle(GetAdminReportQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        // First day of the month, TrendMonths-1 months back — the trend window start.
        var windowStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(TrendMonths - 1));

        // -- Headline totals ----------------------------------------------
        var totalUsers = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
        var totalCourses = await _db.Courses.AsNoTracking().CountAsync(cancellationToken);
        var totalEnrollments = await _db.Enrollments.AsNoTracking().CountAsync(cancellationToken);
        var totalCertificates = await _db.Certificates.AsNoTracking().CountAsync(cancellationToken);

        // -- Registration trend (grouped by year+month, split by role) ----
        var regGroups = await _db.Users
            .AsNoTracking()
            .Where(u => u.CreatedAt >= windowStart)
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Students = g.Count(u => u.Role == "Student"),
                Teachers = g.Count(u => u.Role == "Teacher"),
                Total = g.Count(),
            })
            .ToListAsync(cancellationToken);

        var registrationTrend = BuildMonths(windowStart, now)
            .Select(m =>
            {
                var hit = regGroups.FirstOrDefault(g => g.Year == m.Year && g.Month == m.Month);
                return new MonthlyRegistrationDto
                {
                    Year = m.Year,
                    Month = m.Month,
                    Label = m.Label,
                    Students = hit?.Students ?? 0,
                    Teachers = hit?.Teachers ?? 0,
                    Total = hit?.Total ?? 0,
                };
            })
            .ToList();

        // -- Enrollment trend ---------------------------------------------
        var enrGroups = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.EnrolledAt >= windowStart)
            .GroupBy(e => new { e.EnrolledAt.Year, e.EnrolledAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var enrollmentTrend = BuildMonths(windowStart, now)
            .Select(m => new MonthlyCountDto
            {
                Year = m.Year,
                Month = m.Month,
                Label = m.Label,
                Count = enrGroups.FirstOrDefault(g => g.Year == m.Year && g.Month == m.Month)?.Count ?? 0,
            })
            .ToList();

        // -- Category breakdown -------------------------------------------
        var categoryGroups = await _db.Courses
            .AsNoTracking()
            .GroupBy(c => c.Category)
            .Select(g => new
            {
                Category = g.Key,
                CourseCount = g.Count(),
                PublishedCount = g.Count(c => c.IsPublished),
                EnrollmentCount = g.SelectMany(c => c.Enrollments).Count(),
                CompletedCount = g.SelectMany(c => c.Enrollments).Count(e => e.IsCompleted),
            })
            .ToListAsync(cancellationToken);

        var categories = categoryGroups
            .OrderByDescending(c => c.EnrollmentCount)
            .ThenByDescending(c => c.CourseCount)
            .Select(c => new CategoryReportDto
            {
                Category = c.Category,
                CourseCount = c.CourseCount,
                PublishedCount = c.PublishedCount,
                EnrollmentCount = c.EnrollmentCount,
                CompletionRate = c.EnrollmentCount == 0
                    ? 0
                    : (int)Math.Round(100.0 * c.CompletedCount / c.EnrollmentCount),
            })
            .ToList();

        // -- Top teachers by students reached -----------------------------
        var teacherGroups = await _db.Courses
            .AsNoTracking()
            .GroupBy(c => new { c.TeacherId, c.Teacher.FirstName, c.Teacher.LastName, c.Teacher.Email })
            .Select(g => new
            {
                g.Key.TeacherId,
                g.Key.FirstName,
                g.Key.LastName,
                g.Key.Email,
                CourseCount = g.Count(),
                PublishedCount = g.Count(c => c.IsPublished),
                // Distinct students across all of this teacher's courses.
                TotalStudents = g.SelectMany(c => c.Enrollments).Select(e => e.StudentId).Distinct().Count(),
            })
            .ToListAsync(cancellationToken);

        var topTeachers = teacherGroups
            .OrderByDescending(t => t.TotalStudents)
            .ThenByDescending(t => t.PublishedCount)
            .Take(5)
            .Select(t => new TopTeacherReportDto
            {
                TeacherId = t.TeacherId,
                Name = (t.FirstName + " " + t.LastName).Trim(),
                Email = t.Email,
                CourseCount = t.CourseCount,
                PublishedCount = t.PublishedCount,
                TotalStudents = t.TotalStudents,
            })
            .ToList();

        // -- Completion funnel --------------------------------------------
        var startedCount = await _db.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.ProgressPercentage > 0, cancellationToken);
        var completedCount = await _db.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.IsCompleted, cancellationToken);

        return new AdminReportDto
        {
            GeneratedAt = now,
            TotalUsers = totalUsers,
            TotalCourses = totalCourses,
            TotalEnrollments = totalEnrollments,
            TotalCertificates = totalCertificates,
            RegistrationTrend = registrationTrend,
            EnrollmentTrend = enrollmentTrend,
            Categories = categories,
            TopTeachers = topTeachers,
            Funnel = new CompletionFunnelDto
            {
                Enrolled = totalEnrollments,
                Started = startedCount,
                Completed = completedCount,
                Certified = totalCertificates,
            },
        };
    }

    /// <summary>Enumerates every month in [windowStart, now], inclusive, oldest first.</summary>
    private static IEnumerable<(int Year, int Month, string Label)> BuildMonths(DateTime windowStart, DateTime now)
    {
        var cursor = new DateTime(windowStart.Year, windowStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        while (cursor <= end)
        {
            // "Jan" normally; "Jan '26" only on the January boundary so the axis stays readable.
            var label = cursor.Month == 1
                ? $"{cursor.ToString("MMM", CultureInfo.InvariantCulture)} '{cursor:yy}"
                : cursor.ToString("MMM", CultureInfo.InvariantCulture);
            yield return (cursor.Year, cursor.Month, label);
            cursor = cursor.AddMonths(1);
        }
    }
}
