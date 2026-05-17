using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

/// <summary>
/// Clone a course as a fresh draft owned by the caller. Modules, lessons,
/// assessments, and questions are deep-copied; enrollments, submissions,
/// lesson progress, certificates, announcements, and co-instructor links
/// are NOT — the copy starts clean. Title gets a "(copy)" suffix unless the
/// caller supplied a new title.
/// </summary>
public record DuplicateCourseCommand(Guid SourceCourseId, string? NewTitle)
    : IRequest<CreatedCourseDto>;

public class DuplicateCourseCommandHandler
    : IRequestHandler<DuplicateCourseCommand, CreatedCourseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DuplicateCourseCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CreatedCourseDto> Handle(DuplicateCourseCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        // Primary OR co-instructor can duplicate (they all know the content).
        var src = await _db.Courses
            .AsNoTracking()
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons)
            .Include(c => c.Assessments)
                .ThenInclude(a => a.Questions)
            .FirstOrDefaultAsync(c => c.Id == request.SourceCourseId
                && (c.TeacherId == teacherId
                    || c.CoInstructors.Any(ci => ci.UserId == teacherId)), ct)
            ?? throw new KeyNotFoundException($"Course {request.SourceCourseId} was not found.");

        var now = DateTime.UtcNow;
        var copy = new Course
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,                  // caller becomes the primary
            OrganizationId = src.OrganizationId,    // stay in the same tenant
            BranchId = src.BranchId,
            Title = !string.IsNullOrWhiteSpace(request.NewTitle)
                ? request.NewTitle.Trim()
                : $"{src.Title} (copy)",
            Description = src.Description,
            Category = src.Category,
            ThumbnailUrl = src.ThumbnailUrl,
            MaxStudents = src.MaxStudents,
            IsPublished = false,                    // copy starts as a draft
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var srcModule in src.Modules.OrderBy(m => m.Order))
        {
            var newModule = new Module
            {
                Id = Guid.NewGuid(),
                CourseId = copy.Id,
                Title = srcModule.Title,
                Description = srcModule.Description,
                Order = srcModule.Order,
                CreatedAt = now,
            };
            foreach (var srcLesson in srcModule.Lessons.OrderBy(l => l.Order))
            {
                newModule.Lessons.Add(new Lesson
                {
                    Id = Guid.NewGuid(),
                    ModuleId = newModule.Id,
                    Title = srcLesson.Title,
                    Type = srcLesson.Type,
                    Content = srcLesson.Content, // same video URL / text body
                    Duration = srcLesson.Duration,
                    Order = srcLesson.Order,
                    IsPublished = srcLesson.IsPublished,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            copy.Modules.Add(newModule);
        }

        foreach (var srcAssessment in src.Assessments)
        {
            var newAssessment = new Assessment
            {
                Id = Guid.NewGuid(),
                CourseId = copy.Id,
                Title = srcAssessment.Title,
                Type = srcAssessment.Type,
                TimeLimit = srcAssessment.TimeLimit,
                PassingScore = srcAssessment.PassingScore,
                DueDate = null, // don't carry stale due dates into a copy
                MaxAttempts = srcAssessment.MaxAttempts,
                CreatedAt = now,
            };
            foreach (var srcQ in srcAssessment.Questions.OrderBy(q => q.Order))
            {
                newAssessment.Questions.Add(new Question
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = newAssessment.Id,
                    QuestionText = srcQ.QuestionText,
                    Type = srcQ.Type,
                    Options = srcQ.Options,
                    CorrectAnswer = srcQ.CorrectAnswer,
                    Points = srcQ.Points,
                    Order = srcQ.Order,
                });
            }
            copy.Assessments.Add(newAssessment);
        }

        _db.Courses.Add(copy);
        await _db.SaveChangesAsync(ct);

        return new CreatedCourseDto
        {
            CourseId = copy.Id,
            Title = copy.Title,
            Description = copy.Description,
            Category = copy.Category,
            ThumbnailUrl = copy.ThumbnailUrl,
            MaxStudents = copy.MaxStudents,
            IsPublished = copy.IsPublished,
            CreatedAt = copy.CreatedAt,
        };
    }
}
