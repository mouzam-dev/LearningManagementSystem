using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

/// <summary>
/// "My courses" page. Returns courses where the caller is either the primary
/// teacher OR a co-instructor. Archived courses are excluded by default — pass
/// <c>IncludeArchived = true</c> to show them.
/// </summary>
public record GetTeacherCoursesQuery(bool IncludeArchived = false)
    : IRequest<IReadOnlyList<TeacherCourseListItemDto>>;

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

        var query = _db.Courses
            .AsNoTracking()
            .Where(c => c.TeacherId == teacherId
                     || c.CoInstructors.Any(ci => ci.UserId == teacherId));

        if (!request.IncludeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        return await query
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
                IsArchived = c.IsArchived,
                IsPrimaryTeacher = c.TeacherId == teacherId,
                ModuleCount = c.Modules.Count,
                LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(l => l.IsPublished),
                AssessmentCount = c.Assessments.Count,
                StudentCount = c.Enrollments.Count,
                CoInstructorCount = c.CoInstructors.Count,
                AverageProgress = c.Enrollments.Any()
                    ? Math.Round(c.Enrollments.Average(e => e.ProgressPercentage), 1)
                    : 0m,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
