using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

public record GetDashboardQuery() : IRequest<DashboardDto>;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private const int MaxUpcomingDeadlines = 5;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var enrolled = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new CourseProgressDto
            {
                CourseId = e.CourseId,
                Title = e.Course.Title,
                ThumbnailUrl = e.Course.ThumbnailUrl,
                TeacherName = (e.Course.Teacher.FirstName + " " + e.Course.Teacher.LastName).Trim(),
                ProgressPercentage = e.ProgressPercentage,
                IsCompleted = e.IsCompleted,
                EnrolledAt = e.EnrolledAt,
            })
            .ToListAsync(cancellationToken);

        var totalCourses = enrolled.Count;
        var completedCourses = enrolled.Count(c => c.IsCompleted);
        var overallProgress = totalCourses > 0
            ? Math.Round(enrolled.Average(c => c.ProgressPercentage), 1)
            : 0m;

        var enrolledCourseIds = enrolled.Select(c => c.CourseId).ToList();

        var now = DateTime.UtcNow;
        var deadlines = await _db.Assessments
            .AsNoTracking()
            .Where(a => a.DueDate != null
                        && a.DueDate > now
                        && enrolledCourseIds.Contains(a.CourseId))
            .OrderBy(a => a.DueDate)
            .Take(MaxUpcomingDeadlines)
            .Select(a => new DeadlineDto
            {
                AssessmentId = a.Id,
                CourseId = a.CourseId,
                Title = a.Title,
                CourseTitle = a.Course.Title,
                Type = a.Type,
                DueDate = a.DueDate!.Value,
            })
            .ToListAsync(cancellationToken);

        return new DashboardDto
        {
            TotalCourses = totalCourses,
            CompletedCourses = completedCourses,
            OverallProgress = overallProgress,
            EnrolledCourses = enrolled,
            UpcomingDeadlines = deadlines,
        };
    }
}
