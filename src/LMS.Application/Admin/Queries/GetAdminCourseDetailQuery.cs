using LMS.Application.Admin.Dtos;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Queries;

public record GetAdminCourseDetailQuery(Guid CourseId) : IRequest<AdminCourseDetailDto>;

public class GetAdminCourseDetailQueryHandler
    : IRequestHandler<GetAdminCourseDetailQuery, AdminCourseDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetAdminCourseDetailQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminCourseDetailDto> Handle(GetAdminCourseDetailQuery request, CancellationToken cancellationToken)
    {
        var course = await _db.Courses
            .AsNoTracking()
            .Include(c => c.Teacher)
            .Include(c => c.Modules.OrderBy(m => m.Order))
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        var enrollments = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == course.Id)
            .Select(e => new { e.IsCompleted, e.ProgressPercentage })
            .ToListAsync(cancellationToken);

        var assessmentCount = await _db.Assessments
            .AsNoTracking()
            .CountAsync(a => a.CourseId == course.Id, cancellationToken);

        var certificatesIssued = await _db.Certificates
            .AsNoTracking()
            .CountAsync(c => c.CourseId == course.Id, cancellationToken);

        var studentCount = enrollments.Count;
        var completedCount = enrollments.Count(e => e.IsCompleted);
        var averageProgress = studentCount == 0
            ? 0m
            : Math.Round(enrollments.Average(e => e.ProgressPercentage), 1);

        return new AdminCourseDetailDto
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
            ModuleCount = course.Modules.Count,
            LessonCount = course.Modules.SelectMany(m => m.Lessons).Count(),
            AssessmentCount = assessmentCount,
            StudentCount = studentCount,
            CompletedCount = completedCount,
            CertificatesIssued = certificatesIssued,
            AverageProgress = averageProgress,
            Modules = course.Modules
                .OrderBy(m => m.Order)
                .Select(m => new AdminCourseModuleDto
                {
                    ModuleId = m.Id,
                    Title = m.Title,
                    Order = m.Order,
                    LessonCount = m.Lessons.Count,
                })
                .ToList(),
        };
    }
}
