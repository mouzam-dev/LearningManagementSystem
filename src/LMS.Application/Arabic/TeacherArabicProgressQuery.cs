using FluentValidation;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Arabic;

/// <summary>One student's Learn Arabic standing, for the teacher's progress page.</summary>
public record TeacherArabicStudentDto(
    Guid StudentId, string Name, string Email,
    int LessonsDone, int UnitTestsDone, int? AverageScore, int Attempts,
    DateTime? LastActivity, int? LastBook, int? LastUnit, int? LastLesson);

public record TeacherArabicProgressDto(int TotalLessons, int TotalUnits, PagedResult<TeacherArabicStudentDto> Students);

/// <summary>
/// Arabic progress of every student enrolled in one of the current teacher's courses
/// (owner or co-instructor), most recently active first. Students who haven't started are included.
/// </summary>
public record GetTeacherArabicProgressQuery(string? Search, int Page = 1, int PageSize = 25) : IRequest<TeacherArabicProgressDto>;

public class GetTeacherArabicProgressQueryValidator : AbstractValidator<GetTeacherArabicProgressQuery>
{
    public GetTeacherArabicProgressQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public class GetTeacherArabicProgressQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetTeacherArabicProgressQuery, TeacherArabicProgressDto>
{
    public async Task<TeacherArabicProgressDto> Handle(GetTeacherArabicProgressQuery request, CancellationToken ct)
    {
        var teacherId = currentUser.GetUserId();

        var studentIds = db.Enrollments.AsNoTracking()
            .Where(e => e.Course.TeacherId == teacherId || e.Course.CoInstructors.Any(ci => ci.UserId == teacherId))
            .Select(e => e.StudentId)
            .Distinct();

        var students = db.Users.AsNoTracking().Where(u => studentIds.Contains(u.Id));
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            students = students.Where(u => (u.FirstName + " " + u.LastName).ToLower().Contains(s) || u.Email.ToLower().Contains(s));
        }

        var total = await students.CountAsync(ct);
        var page = await students
            .Select(u => new
            {
                u.Id,
                Name = u.FirstName + " " + u.LastName,
                u.Email,
                Last = db.ArabicProgress.Where(p => p.UserId == u.Id).Max(p => (DateTime?)p.UpdatedAt),
            })
            .OrderByDescending(s => s.Last.HasValue).ThenByDescending(s => s.Last).ThenBy(s => s.Name)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ToListAsync(ct);

        // Progress rows for this page only (a few hundred at most), summarised in memory.
        var ids = page.Select(s => s.Id).ToList();
        var rows = await db.ArabicProgress.AsNoTracking().Where(p => ids.Contains(p.UserId))
            .Select(p => new { p.UserId, p.UnitId, p.LessonNumber, p.BestScore, p.Attempts, p.UpdatedAt, p.Unit.BookNumber, p.Unit.UnitNumber })
            .ToListAsync(ct);
        var byStudent = rows.ToLookup(r => r.UserId);

        var items = page.Select(s =>
        {
            var mine = byStudent[s.Id].ToList();
            var lessons = mine.Where(r => r.LessonNumber > 0).ToList();
            var last = mine.MaxBy(r => r.UpdatedAt);
            return new TeacherArabicStudentDto(
                s.Id, s.Name, s.Email,
                lessons.Count,
                mine.Count(r => r.LessonNumber == 0),
                lessons.Count > 0 ? (int)Math.Round(lessons.Average(r => r.BestScore)) : null,
                mine.Sum(r => r.Attempts),
                last?.UpdatedAt, last?.BookNumber, last?.UnitNumber, last?.LessonNumber);
        }).ToList();

        return new TeacherArabicProgressDto(
            await db.ArabicLessons.CountAsync(ct),
            await db.ArabicUnits.CountAsync(ct),
            new PagedResult<TeacherArabicStudentDto> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = total });
    }
}
