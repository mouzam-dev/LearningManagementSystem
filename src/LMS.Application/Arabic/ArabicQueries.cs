using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Arabic;

/// <summary>
/// The whole course (4 books, 65 units, 588 lessons) in the learn-app's catalog format. Fixed reference
/// content of ~13k rows, so it is loaded in one go rather than paged; the embedded app needs all of it.
/// </summary>
public record GetArabicCatalogQuery() : IRequest<IReadOnlyList<ArabicCatalogBookDto>>;

public class GetArabicCatalogQueryHandler(IApplicationDbContext db) : IRequestHandler<GetArabicCatalogQuery, IReadOnlyList<ArabicCatalogBookDto>>
{
    public async Task<IReadOnlyList<ArabicCatalogBookDto>> Handle(GetArabicCatalogQuery request, CancellationToken ct)
    {
        // One query per table: a single joined query would multiply dialogue x vocab x quiz rows.
        var units = await db.ArabicUnits.AsNoTracking().OrderBy(u => u.BookNumber).ThenBy(u => u.UnitNumber).ToListAsync(ct);
        var lessons = (await db.ArabicLessons.AsNoTracking().OrderBy(l => l.Number).ToListAsync(ct)).ToLookup(l => l.UnitId);
        var dialogue = (await db.ArabicDialogueLines.AsNoTracking().OrderBy(d => d.Order).ToListAsync(ct)).ToLookup(d => d.LessonId);
        var vocab = (await db.ArabicVocabItems.AsNoTracking().OrderBy(v => v.Order).ToListAsync(ct)).ToLookup(v => v.LessonId);
        var quiz = (await db.ArabicQuizQuestions.AsNoTracking().OrderBy(q => q.Order).ToListAsync(ct)).ToLookup(q => q.LessonId);

        return units.GroupBy(u => u.BookNumber).Select(b => new ArabicCatalogBookDto(b.Key, b.Select(u =>
            new ArabicCatalogUnitDto(u.Id, u.BookNumber, u.UnitNumber, new ArabicTextDto(u.TitleAr, u.TitleEn, u.TitleUr),
                lessons[u.Id].Select(l => new ArabicCatalogLessonDto(
                    l.Number,
                    new ArabicTextDto(l.TitleAr, l.TitleEn, l.TitleUr),
                    new ArabicTextDto(l.IntroAr, l.IntroEn, l.IntroUr),
                    dialogue[l.Id].Select(d => new ArabicCatalogLineDto(d.Speaker, d.Ar, d.Tr, d.En, d.Ur, d.Source, d.Audio)).ToList(),
                    vocab[l.Id].Select(v => new ArabicCatalogWordDto(v.Ar, v.Tr, v.En, v.Ur, v.Audio)).ToList(),
                    quiz[l.Id].Select(q => new ArabicCatalogQuizDto(
                        new ArabicTextDto(q.QuestionAr, q.QuestionEn, q.QuestionUr), q.Options, q.OptionsAr, q.AnswerIndex)).ToList()
                )).ToList())).ToList())).ToList();
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
