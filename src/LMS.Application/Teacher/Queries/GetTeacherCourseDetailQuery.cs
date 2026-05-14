using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetTeacherCourseDetailQuery(Guid CourseId) : IRequest<TeacherCourseDetailDto>;

public class GetTeacherCourseDetailQueryHandler
    : IRequestHandler<GetTeacherCourseDetailQuery, TeacherCourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTeacherCourseDetailQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherCourseDetailDto> Handle(GetTeacherCourseDetailQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var course = await _db.Courses
            .AsNoTracking()
            .Include(c => c.Modules.OrderBy(m => m.Order))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.Order))
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        if (course.TeacherId != teacherId)
        {
            // Same 404 message as "doesn't exist" so other teachers can't probe ids.
            throw new KeyNotFoundException($"Course {request.CourseId} was not found.");
        }

        var studentCount = await _db.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.CourseId == course.Id, cancellationToken);

        var assessments = await _db.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == course.Id)
            .OrderBy(a => a.DueDate ?? DateTime.MaxValue)
            .ThenBy(a => a.Title)
            .Select(a => new TeacherAssessmentListItemDto
            {
                AssessmentId = a.Id,
                CourseId = a.CourseId,
                Title = a.Title,
                Type = a.Type,
                TimeLimit = a.TimeLimit,
                PassingScore = a.PassingScore,
                DueDate = a.DueDate,
                MaxAttempts = a.MaxAttempts,
                QuestionCount = a.Questions.Count,
                TotalPoints = a.Questions.Sum(q => q.Points),
                SubmissionCount = a.Submissions.Count,
                UngradedCount = a.Submissions.Count(s => s.Score == null),
            })
            .ToListAsync(cancellationToken);

        var assessmentCount = assessments.Count;

        // AverageAsync on a nullable projection returns null when the set is
        // empty — that translates cleanly to SQL AVG(...) which also returns
        // NULL on an empty result. (Earlier .DefaultIfEmpty(0m).AverageAsync()
        // doesn't translate.)
        var averageProgress = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == course.Id)
            .AverageAsync(e => (decimal?)e.ProgressPercentage, cancellationToken)
            ?? 0m;

        return new TeacherCourseDetailDto
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
            StudentCount = studentCount,
            AssessmentCount = assessmentCount,
            AverageProgress = Math.Round(averageProgress, 1),
            Modules = course.Modules
                .OrderBy(m => m.Order)
                .Select(m => new TeacherModuleDto
                {
                    ModuleId = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    Order = m.Order,
                    Lessons = m.Lessons
                        .OrderBy(l => l.Order)
                        .Select(l => new TeacherLessonDto
                        {
                            LessonId = l.Id,
                            Title = l.Title,
                            Type = l.Type,
                            Content = l.Content,
                            Duration = l.Duration,
                            Order = l.Order,
                            IsPublished = l.IsPublished,
                        })
                        .ToList(),
                })
                .ToList(),
            Assessments = assessments,
        };
    }
}
