using LMS.Application.Admin.Dtos;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Queries;

public record GetAdminUserDetailQuery(Guid UserId) : IRequest<AdminUserDetailDto>;

public class GetAdminUserDetailQueryHandler
    : IRequestHandler<GetAdminUserDetailQuery, AdminUserDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetAdminUserDetailQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminUserDetailDto> Handle(GetAdminUserDetailQuery request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} was not found.");

        var coursesTaught = user.Role == "Teacher"
            ? await _db.Courses.CountAsync(c => c.TeacherId == user.Id, cancellationToken)
            : 0;
        var publishedCourses = user.Role == "Teacher"
            ? await _db.Courses.CountAsync(c => c.TeacherId == user.Id && c.IsPublished, cancellationToken)
            : 0;
        var totalStudentsAcrossCourses = user.Role == "Teacher"
            ? await _db.Enrollments
                .Where(e => e.Course.TeacherId == user.Id)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync(cancellationToken)
            : 0;

        var enrollments = user.Role == "Student"
            ? await _db.Enrollments.CountAsync(e => e.StudentId == user.Id, cancellationToken)
            : 0;
        var completedCourses = user.Role == "Student"
            ? await _db.Enrollments.CountAsync(e => e.StudentId == user.Id && e.IsCompleted, cancellationToken)
            : 0;
        var certificatesEarned = await _db.Certificates.CountAsync(c => c.UserId == user.Id, cancellationToken);
        var submissions = user.Role == "Student"
            ? await _db.Submissions.CountAsync(s => s.StudentId == user.Id, cancellationToken)
            : 0;

        // Tenancy labels — fetched separately so the projection above stays clean.
        string? organizationName = null;
        if (user.OrganizationId.HasValue)
        {
            organizationName = await _db.Organizations
                .Where(o => o.Id == user.OrganizationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }
        string? branchName = null;
        if (user.BranchId.HasValue)
        {
            branchName = await _db.Branches
                .Where(b => b.Id == user.BranchId)
                .Select(b => b.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new AdminUserDetailDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            Bio = user.Bio,
            ProfilePictureUrl = user.ProfilePictureUrl,
            IsActive = user.IsActive,
            IsVerified = user.IsVerified,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            OrganizationId = user.OrganizationId,
            OrganizationName = organizationName,
            BranchId = user.BranchId,
            BranchName = branchName,
            CoursesTaught = coursesTaught,
            PublishedCourses = publishedCourses,
            TotalStudentsAcrossCourses = totalStudentsAcrossCourses,
            Enrollments = enrollments,
            CompletedCourses = completedCourses,
            CertificatesEarned = certificatesEarned,
            Submissions = submissions,
        };
    }
}
