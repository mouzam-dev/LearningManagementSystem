using LMS.Application.Admin.Dtos;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Queries;

// ---------------- Users export --------------------------------------------

public record GetUsersExportQuery() : IRequest<IReadOnlyList<UserExportRow>>;

public class GetUsersExportQueryHandler
    : IRequestHandler<GetUsersExportQuery, IReadOnlyList<UserExportRow>>
{
    private readonly IApplicationDbContext _db;

    public GetUsersExportQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserExportRow>> Handle(GetUsersExportQuery request, CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserExportRow
            {
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                IsVerified = u.IsVerified,
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}

// ---------------- Courses export ------------------------------------------

public record GetCoursesExportQuery() : IRequest<IReadOnlyList<CourseExportRow>>;

public class GetCoursesExportQueryHandler
    : IRequestHandler<GetCoursesExportQuery, IReadOnlyList<CourseExportRow>>
{
    private readonly IApplicationDbContext _db;

    public GetCoursesExportQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CourseExportRow>> Handle(GetCoursesExportQuery request, CancellationToken cancellationToken)
    {
        return await _db.Courses
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CourseExportRow
            {
                Title = c.Title,
                Category = c.Category,
                TeacherName = (c.Teacher.FirstName + " " + c.Teacher.LastName).Trim(),
                TeacherEmail = c.Teacher.Email,
                IsPublished = c.IsPublished,
                ModuleCount = c.Modules.Count,
                LessonCount = c.Modules.SelectMany(m => m.Lessons).Count(),
                StudentCount = c.Enrollments.Count,
                CreatedAt = c.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
