using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetTeacherCoursesQuery() : IRequest<IReadOnlyList<TeacherCourseListItemDto>>;

public class GetTeacherCoursesQueryHandler
    : IRequestHandler<GetTeacherCoursesQuery, IReadOnlyList<TeacherCourseListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTeacherCoursesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TeacherCourseListItemDto>> Handle(
        GetTeacherCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        return await _db.Courses
            .AsNoTracking()
            .Where(c => c.TeacherId == teacherId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new TeacherCourseListItemDto
            {
                CourseId = c.Id,
                Title = c.Title,
                Description = c.Description,
                Category = c.Category,
                ThumbnailUrl = c.ThumbnailUrl,
                MaxStudents = c.MaxStudents,
                IsPublished = c.IsPublished,
                ModuleCount = c.Modules.Count,
                LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(l => l.IsPublished),
                AssessmentCount = c.Assessments.Count,
                StudentCount = c.Enrollments.Count,
                AverageProgress = c.Enrollments.Any()
                    ? Math.Round(c.Enrollments.Average(e => e.ProgressPercentage), 1)
                    : 0m,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
