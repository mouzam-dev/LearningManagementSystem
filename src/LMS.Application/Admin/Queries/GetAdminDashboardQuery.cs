using LMS.Application.Admin.Dtos;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Queries;

public record GetAdminDashboardQuery() : IRequest<AdminDashboardDto>;

public class GetAdminDashboardQueryHandler
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IApplicationDbContext _db;

    public GetAdminDashboardQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        // -- User counts ---------------------------------------------------
        var users = _db.Users.AsNoTracking();
        var totalUsers = await users.CountAsync(cancellationToken);
        var activeUsers = await users.CountAsync(u => u.IsActive, cancellationToken);
        var students = await users.CountAsync(u => u.Role == "Student", cancellationToken);
        var teachers = await users.CountAsync(u => u.Role == "Teacher", cancellationToken);
        var admins = await users.CountAsync(u => u.Role == "Admin", cancellationToken);
        var recent7 = await users.CountAsync(u => u.CreatedAt >= sevenDaysAgo, cancellationToken);

        // -- Course / learning -------------------------------------------
        var courses = _db.Courses.AsNoTracking();
        var totalCourses = await courses.CountAsync(cancellationToken);
        var publishedCourses = await courses.CountAsync(c => c.IsPublished, cancellationToken);

        var enrollments = _db.Enrollments.AsNoTracking();
        var totalEnrollments = await enrollments.CountAsync(cancellationToken);
        var completedEnrollments = await enrollments.CountAsync(e => e.IsCompleted, cancellationToken);

        var certificatesIssued = await _db.Certificates.AsNoTracking().CountAsync(cancellationToken);
        var totalSubmissions = await _db.Submissions.AsNoTracking().CountAsync(cancellationToken);
        var ungradedSubmissions = await _db.Submissions.AsNoTracking().CountAsync(s => s.Score == null, cancellationToken);

        // -- Recent registrations + top courses --------------------------
        var recentRegistrations = await users
            .OrderByDescending(u => u.CreatedAt)
            .Take(8)
            .Select(u => new RecentRegistrationDto
            {
                UserId = u.Id,
                Name = (u.FirstName + " " + u.LastName).Trim(),
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var topCourses = await courses
            .OrderByDescending(c => c.Enrollments.Count)
            .ThenByDescending(c => c.UpdatedAt)
            .Take(5)
            .Select(c => new TopCourseDto
            {
                CourseId = c.Id,
                Title = c.Title,
                Category = c.Category,
                TeacherName = (c.Teacher.FirstName + " " + c.Teacher.LastName).Trim(),
                StudentCount = c.Enrollments.Count,
                IsPublished = c.IsPublished,
            })
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto
        {
            Summary = new AdminDashboardSummary
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                InactiveUsers = totalUsers - activeUsers,
                Students = students,
                Teachers = teachers,
                Admins = admins,
                RegistrationsLast7Days = recent7,
                TotalCourses = totalCourses,
                PublishedCourses = publishedCourses,
                DraftCourses = totalCourses - publishedCourses,
                TotalEnrollments = totalEnrollments,
                CompletedEnrollments = completedEnrollments,
                CertificatesIssued = certificatesIssued,
                TotalSubmissions = totalSubmissions,
                UngradedSubmissions = ungradedSubmissions,
            },
            RecentRegistrations = recentRegistrations,
            TopCourses = topCourses,
        };
    }
}
