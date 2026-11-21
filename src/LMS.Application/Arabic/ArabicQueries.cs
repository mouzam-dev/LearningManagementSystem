using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Arabic;

/// <summary>All books with their units (titles + lesson counts). Anonymous; small, fixed-size list.</summary>
public record GetArabicBooksQuery() : IRequest<IReadOnlyList<ArabicBookDto>>;

public class GetArabicBooksQueryHandler(IApplicationDbContext db) : IRequestHandler<GetArabicBooksQuery, IReadOnlyList<ArabicBookDto>>
{
    public async Task<IReadOnlyList<ArabicBookDto>> Handle(GetArabicBooksQuery request, CancellationToken ct)
    {
        var units = await db.ArabicUnits.AsNoTracking()
            .OrderBy(u => u.BookNumber).ThenBy(u => u.UnitNumber)
            .Select(u => new { u.BookNumber, Dto = new ArabicUnitSummaryDto(u.Id, u.UnitNumber, u.TitleAr, u.TitleEn, u.TitleUr, u.Lessons.Count) })
            .ToListAsync(ct);
        return units.GroupBy(u => u.BookNumber)
            .Select(g => new ArabicBookDto(g.Key, g.Select(x => x.Dto).ToList()))
            .ToList();
    }
}

/// <summary>One unit with every lesson's intro, dialogue, vocabulary and quiz.</summary>
public record GetArabicUnitQuery(string UnitId) : IRequest<ArabicUnitDto>;

public class GetArabicUnitQueryHandler(IApplicationDbContext db) : IRequestHandler<GetArabicUnitQuery, ArabicUnitDto>
{
    public async Task<ArabicUnitDto> Handle(GetArabicUnitQuery request, CancellationToken ct)
    {
        var u = await db.ArabicUnits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.UnitId, ct)
            ?? throw new KeyNotFoundException($"Arabic unit {request.UnitId} was not found.");

        // One query per child table: a single joined query would multiply dialogue x vocab x quiz rows.
        var lessons = await db.ArabicLessons.AsNoTracking().Where(l => l.UnitId == u.Id).OrderBy(l => l.Number).ToListAsync(ct);
        var ids = lessons.Select(l => l.Id).ToList();
        var dialogue = (await db.ArabicDialogueLines.AsNoTracking().Where(d => ids.Contains(d.LessonId)).OrderBy(d => d.Order).ToListAsync(ct))
            .ToLookup(d => d.LessonId);
        var vocab = (await db.ArabicVocabItems.AsNoTracking().Where(v => ids.Contains(v.LessonId)).OrderBy(v => v.Order).ToListAsync(ct))
            .ToLookup(v => v.LessonId);
        var quiz = (await db.ArabicQuizQuestions.AsNoTracking().Where(q => ids.Contains(q.LessonId)).OrderBy(q => q.Order).ToListAsync(ct))
            .ToLookup(q => q.LessonId);

        return new ArabicUnitDto(u.Id, u.BookNumber, u.UnitNumber, u.TitleAr, u.TitleEn, u.TitleUr,
            lessons.Select(l => new ArabicLessonDto(
                l.Number, l.TitleAr, l.TitleEn, l.TitleUr, l.IntroAr, l.IntroEn, l.IntroUr,
                dialogue[l.Id].Select(d => new ArabicLineDto(d.Speaker, d.Ar, d.Tr, d.En, d.Ur)).ToList(),
                vocab[l.Id].Select(v => new ArabicWordDto(v.Ar, v.Tr, v.En, v.Ur)).ToList(),
                quiz[l.Id].Select(q => new ArabicQuizDto(q.QuestionAr, q.QuestionEn, q.QuestionUr, q.Options, q.OptionsAr, q.AnswerIndex)).ToList()
            )).ToList());
    }
}

/// <summary>Vocabulary for one book, or one unit of it. Bounded by the book (~2k words), so not paged.</summary>
public record GetArabicWordsQuery(int Book, string? UnitId) : IRequest<IReadOnlyList<ArabicWordCardDto>>;

public class GetArabicWordsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetArabicWordsQuery, IReadOnlyList<ArabicWordCardDto>>
{
    public async Task<IReadOnlyList<ArabicWordCardDto>> Handle(GetArabicWordsQuery request, CancellationToken ct)
    {
        var q = db.ArabicVocabItems.AsNoTracking().Where(v => v.Lesson.Unit.BookNumber == request.Book);
        if (!string.IsNullOrEmpty(request.UnitId)) q = q.Where(v => v.Lesson.UnitId == request.UnitId);
        return await q
            .OrderBy(v => v.Lesson.Unit.UnitNumber).ThenBy(v => v.Lesson.Number).ThenBy(v => v.Order)
            .Select(v => new ArabicWordCardDto(v.Ar, v.Tr, v.En, v.Ur, v.Lesson.UnitId, v.Lesson.Unit.UnitNumber, v.Lesson.Number))
            .ToListAsync(ct);
    }
}

/// <summary>The signed-in learner's best scores across all units.</summary>
public record GetMyArabicProgressQuery() : IRequest<IReadOnlyList<ArabicProgressDto>>;

public class GetMyArabicProgressQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyArabicProgressQuery, IReadOnlyList<ArabicProgressDto>>
{
    public async Task<IReadOnlyList<ArabicProgressDto>> Handle(GetMyArabicProgressQuery request, CancellationToken ct)
    {
        var userId = currentUser.GetUserId();
        return await db.ArabicProgress.AsNoTracking()
            .Where(p => p.UserId == userId)
            .Select(p => new ArabicProgressDto(p.UnitId, p.LessonNumber, p.BestScore, p.Attempts, p.UpdatedAt))
            .ToListAsync(ct);
    }
}
