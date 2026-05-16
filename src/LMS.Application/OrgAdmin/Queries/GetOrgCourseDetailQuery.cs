using LMS.Application.Common;
using LMS.Application.OrgAdmin.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.OrgAdmin.Queries;

public record GetOrgCourseDetailQuery(Guid CourseId) : IRequest<OrgAdminCourseDetailDto>;

public class GetOrgCourseDetailQueryHandler
    : IRequestHandler<GetOrgCourseDetailQuery, OrgAdminCourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrgCourseDetailQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OrgAdminCourseDetailDto> Handle(GetOrgCourseDetailQuery request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var course = await _db.Courses
            .AsNoTracking()
            .Include(c => c.Teacher)
            .Include(c => c.Branch)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(
                c => c.Id == request.CourseId && c.OrganizationId == orgId, ct)
            ?? throw new KeyNotFoundException(
                $"Course {request.CourseId} was not found in your organization.");

        var enrollments = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == course.Id)
            .Select(e => new { e.IsCompleted, e.ProgressPercentage })
            .ToListAsync(ct);

        var assessmentCount = await _db.Assessments
            .AsNoTracking()
            .CountAsync(a => a.CourseId == course.Id, ct);

        var studentCount = enrollments.Count;
        var completedCount = enrollments.Count(e => e.IsCompleted);
        var averageProgress = studentCount == 0
            ? 0m
            : Math.Round(enrollments.Average(e => e.ProgressPercentage), 1);

        return new OrgAdminCourseDetailDto
        {
            CourseId = course.Id,
            Title = course.Title,
            Description = course.Description,
            Category = course.Category,
            ThumbnailUrl = course.ThumbnailUrl,
            MaxStudents = course.MaxStudents,
            IsPublished = course.IsPublished,
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt,
            TeacherId = course.TeacherId,
            TeacherName = (course.Teacher.FirstName + " " + course.Teacher.LastName).Trim(),
            TeacherEmail = course.Teacher.Email,
            BranchId = course.BranchId,
            BranchName = course.Branch?.Name,
            ModuleCount = course.Modules.Count,
            LessonCount = course.Modules.SelectMany(m => m.Lessons).Count(),
            AssessmentCount = assessmentCount,
            StudentCount = studentCount,
            CompletedCount = completedCount,
            AverageProgress = averageProgress,
        };
    }
}
