using System.Text.Json;
using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

public record GetLessonQuery(Guid LessonId) : IRequest<LessonDetailDto>;

public class GetLessonQueryHandler : IRequestHandler<GetLessonQuery, LessonDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetLessonQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LessonDetailDto> Handle(GetLessonQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Module).ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId && l.IsPublished, cancellationToken)
            ?? throw new KeyNotFoundException($"Lesson {request.LessonId} was not found.");

        var course = lesson.Module.Course;

        var enrollment = await _db.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CourseId == course.Id && e.StudentId == userId, cancellationToken);

        if (enrollment is null)
        {
            throw new UnauthorizedAccessException("You must enrol in the course to view its lessons.");
        }

        // Build the full module/lesson tree (used by the player sidebar) and
        // determine prev/next adjacency in syllabus order.
        var modules = await _db.Modules
            .AsNoTracking()
            .Where(m => m.CourseId == course.Id)
            .OrderBy(m => m.Order)
            .Select(m => new ModuleDto
            {
                ModuleId = m.Id,
                Title = m.Title,
                Description = m.Description,
                Order = m.Order,
                Lessons = m.Lessons
                    .Where(l => l.IsPublished)
                    .OrderBy(l => l.Order)
                    .Select(l => new LessonListItemDto
                    {
                        LessonId = l.Id,
                        Title = l.Title,
                        Type = l.Type,
                        Duration = l.Duration,
                        Order = l.Order,
                        IsCompleted = l.Progress.Any(p => p.UserId == userId && p.CompletedAt != null),
                        WatchTimeSeconds = l.Progress
                            .Where(p => p.UserId == userId)
                            .Select(p => (int?)p.WatchTimeSeconds)
                            .FirstOrDefault() ?? 0,
                    })
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        var orderedLessonIds = modules.SelectMany(m => m.Lessons.Select(l => l.LessonId)).ToList();
        var idx = orderedLessonIds.IndexOf(lesson.Id);
        Guid? prev = idx > 0 ? orderedLessonIds[idx - 1] : null;
        Guid? next = idx >= 0 && idx < orderedLessonIds.Count - 1 ? orderedLessonIds[idx + 1] : null;

        var progress = await _db.LessonProgress
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lesson.Id, cancellationToken);

        var parsed = ExtractContent(lesson.Type, lesson.Content);

        return new LessonDetailDto
        {
            LessonId = lesson.Id,
            Title = lesson.Title,
            Type = lesson.Type,
            Duration = lesson.Duration,
            Order = lesson.Order,
            VideoUrl = parsed.VideoUrl,
            Body = parsed.Body,
            DocumentUrl = parsed.DocumentUrl,
            DocumentName = parsed.DocumentName,
            CourseId = course.Id,
            CourseTitle = course.Title,
            ModuleId = lesson.Module.Id,
            ModuleTitle = lesson.Module.Title,
            WatchTimeSeconds = progress?.WatchTimeSeconds ?? 0,
            IsCompleted = progress?.CompletedAt is not null,
            CompletedAt = progress?.CompletedAt,
            PreviousLessonId = prev,
            NextLessonId = next,
            Modules = modules,
        };
    }

    private static (string? VideoUrl, string? Body, string? DocumentUrl, string? DocumentName)
        ExtractContent(string type, string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return (null, null, null, null);
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            string? Str(string prop) =>
                root.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String
                    ? el.GetString()
                    : null;

            return (Str("videoUrl"), Str("body"), Str("documentUrl"), Str("documentName"));
        }
        catch (JsonException)
        {
            // Lesson.Content isn't required to be JSON for non-video types — fall
            // back to treating the raw string as the body.
            return type == "Video"
                ? (null, null, null, null)
                : (null, content, null, null);
        }
    }
}
