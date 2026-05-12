using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using LMS.Application.Student.Queries;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Commands;

public record UpdateLessonProgressCommand(
    Guid LessonId,
    int WatchTimeSeconds,
    bool MarkComplete
) : IRequest<LessonDetailDto>;

public class UpdateLessonProgressCommandHandler
    : IRequestHandler<UpdateLessonProgressCommand, LessonDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public UpdateLessonProgressCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<LessonDetailDto> Handle(UpdateLessonProgressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var lesson = await _db.Lessons
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId && l.IsPublished, cancellationToken)
            ?? throw new KeyNotFoundException($"Lesson {request.LessonId} was not found.");

        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.CourseId == lesson.Module.CourseId && e.StudentId == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("You must enrol in the course to update lesson progress.");

        var progress = await _db.LessonProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lesson.Id, cancellationToken);

        if (progress is null)
        {
            progress = new LessonProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LessonId = lesson.Id,
                WatchTimeSeconds = Math.Max(0, request.WatchTimeSeconds),
            };
            _db.LessonProgress.Add(progress);
        }
        else
        {
            // Watch time monotonically increases — never go backwards if a stale
            // ping arrives after the user has scrubbed back.
            progress.WatchTimeSeconds = Math.Max(progress.WatchTimeSeconds, Math.Max(0, request.WatchTimeSeconds));
        }

        if (request.MarkComplete && progress.CompletedAt is null)
        {
            progress.CompletedAt = DateTime.UtcNow;
        }

        // Persist the lesson-progress change first so the recompute below sees
        // it through normal queries instead of fighting the change tracker.
        await _db.SaveChangesAsync(cancellationToken);

        var courseId = lesson.Module.CourseId;
        var totalLessons = await _db.Lessons
            .CountAsync(l => l.Module.CourseId == courseId && l.IsPublished, cancellationToken);

        var completedLessons = await _db.LessonProgress
            .CountAsync(p => p.UserId == userId
                          && p.CompletedAt != null
                          && p.Lesson.Module.CourseId == courseId
                          && p.Lesson.IsPublished, cancellationToken);

        enrollment.ProgressPercentage = totalLessons == 0
            ? 0m
            : Math.Round((decimal)completedLessons / totalLessons * 100m, 1);

        if (totalLessons > 0 && completedLessons >= totalLessons)
        {
            var firstCompletion = !enrollment.IsCompleted;
            enrollment.IsCompleted = true;
            enrollment.CompletedAt ??= DateTime.UtcNow;

            // Auto-issue a certificate the first time the student finishes
            // every lesson. Unique index on (UserId, CourseId) guarantees we
            // can never issue twice for the same enrolment, but we also gate
            // it on firstCompletion so the no-op case doesn't even attempt
            // the insert.
            if (firstCompletion)
            {
                var alreadyIssued = await _db.Certificates
                    .AnyAsync(c => c.UserId == userId && c.CourseId == courseId, cancellationToken);
                if (!alreadyIssued)
                {
                    _db.Certificates.Add(new Certificate
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        CourseId = courseId,
                        VerifyCode = GenerateVerifyCode(),
                        IssuedAt = DateTime.UtcNow,
                    });
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetLessonQuery(lesson.Id), cancellationToken);
    }

    /// <summary>
    /// 12-character URL-safe verification code (e.g. "K7J3-N9PQ-XR2M").
    /// Random Guid bytes → base32 (no padding, no ambiguous chars) → dashed groups.
    /// </summary>
    private static string GenerateVerifyCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Crockford base32 (no I/L/O/0/1)
        Span<byte> bytes = stackalloc byte[8];
        Random.Shared.NextBytes(bytes);
        Span<char> chars = stackalloc char[12];
        for (var i = 0; i < 12; i++)
        {
            chars[i] = alphabet[bytes[i % 8] % alphabet.Length];
            // Reroll to avoid the same byte producing repeats across the run.
            bytes[i % 8] = (byte)(bytes[i % 8] * 31 + i);
        }
        return $"{new string(chars[..4])}-{new string(chars.Slice(4, 4))}-{new string(chars[8..])}";
    }
}
