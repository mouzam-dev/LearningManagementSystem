using FluentValidation;
using LMS.Application.Common;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Arabic;

/// <summary>
/// Record a quiz result. Keeps the best score per (user, unit, lesson) and counts attempts.
/// LessonNumber 0 = unit test.
/// </summary>
// shortcut: the quiz is scored in the browser (self-practice, not graded), so a learner could post any score; score server-side if results ever count toward certificates.
public record SaveArabicProgressCommand(string UnitId, int LessonNumber, int Score) : IRequest<ArabicProgressDto>;

public class SaveArabicProgressCommandValidator : AbstractValidator<SaveArabicProgressCommand>
{
    public SaveArabicProgressCommandValidator()
    {
        RuleFor(x => x.UnitId).NotEmpty().MaximumLength(10);
        RuleFor(x => x.LessonNumber).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Score).InclusiveBetween(0, 100);
    }
}

public class SaveArabicProgressCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<SaveArabicProgressCommand, ArabicProgressDto>
{
    public async Task<ArabicProgressDto> Handle(SaveArabicProgressCommand request, CancellationToken ct)
    {
        var exists = request.LessonNumber == 0
            ? await db.ArabicUnits.AnyAsync(u => u.Id == request.UnitId, ct)
            : await db.ArabicLessons.AnyAsync(l => l.UnitId == request.UnitId && l.Number == request.LessonNumber, ct);
        if (!exists)
            throw new KeyNotFoundException($"Arabic lesson {request.UnitId}/{request.LessonNumber} was not found.");

        var userId = currentUser.GetUserId();
        var row = await db.ArabicProgress.FirstOrDefaultAsync(
            p => p.UserId == userId && p.UnitId == request.UnitId && p.LessonNumber == request.LessonNumber, ct);
        if (row is null)
        {
            row = new ArabicProgress { UserId = userId, UnitId = request.UnitId, LessonNumber = request.LessonNumber };
            db.ArabicProgress.Add(row);
        }

        row.BestScore = Math.Max(row.BestScore, request.Score);
        row.Attempts++;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new ArabicProgressDto(row.UnitId, row.LessonNumber, row.BestScore, row.Attempts, row.UpdatedAt);
    }
}
