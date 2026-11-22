using FluentValidation;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Analytics;

public record AnalyticsDayDto(DateOnly Day, int Views, int Visitors);
public record AnalyticsPageDto(string Path, int Views, int Visitors);
public record AnalyticsShareDto(string Name, int Views);
public record AnalyticsStudentDto(Guid StudentId, string Name, string Email, int PageViews, int LessonsCompleted, DateTime LastSeen);

public record AdminAnalyticsDto(
    int Days, int PageViews, int Visitors, int SignedInUsers,
    IReadOnlyList<AnalyticsDayDto> Daily,
    IReadOnlyList<AnalyticsPageDto> TopPages,
    IReadOnlyList<AnalyticsShareDto> Devices,
    IReadOnlyList<AnalyticsShareDto> Roles,
    int ActiveStudents, int LessonsCompleted, int QuizSubmissions, int ArabicQuizzes, int NewEnrollments,
    IReadOnlyList<AnalyticsStudentDto> TopStudents);

/// <summary>Traffic and student activity over the last <paramref name="Days"/> days (UTC), for super admins.</summary>
public record GetAdminAnalyticsQuery(int Days = 30) : IRequest<AdminAnalyticsDto>;

public class GetAdminAnalyticsQueryValidator : AbstractValidator<GetAdminAnalyticsQuery>
{
    public GetAdminAnalyticsQueryValidator() => RuleFor(x => x.Days).InclusiveBetween(1, 365);
}

public class GetAdminAnalyticsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetAdminAnalyticsQuery, AdminAnalyticsDto>
{
    public async Task<AdminAnalyticsDto> Handle(GetAdminAnalyticsQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-(request.Days - 1));
        var views = db.PageViews.AsNoTracking().Where(v => v.CreatedAt >= from);

        var totals = await views.GroupBy(_ => 1).Select(g => new
        {
            Views = g.Count(),
            Visitors = g.Select(v => v.VisitorId).Distinct().Count(),
            Users = g.Where(v => v.UserId != null).Select(v => v.UserId).Distinct().Count(),
        }).FirstOrDefaultAsync(ct);

        var byDay = await views.GroupBy(v => v.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Views = g.Count(), Visitors = g.Select(v => v.VisitorId).Distinct().Count() })
            .ToListAsync(ct);
        var daily = Enumerable.Range(0, request.Days).Select(i => from.AddDays(i)).Select(d =>
        {
            var hit = byDay.FirstOrDefault(x => x.Day == d);
            return new AnalyticsDayDto(DateOnly.FromDateTime(d), hit?.Views ?? 0, hit?.Visitors ?? 0);
        }).ToList();

        // Sort on anonymous projections: EF can't order by a record built inside the query.
        var topPages = (await views.GroupBy(v => v.Path)
            .Select(g => new { Path = g.Key, Views = g.Count(), Visitors = g.Select(v => v.VisitorId).Distinct().Count() })
            .OrderByDescending(p => p.Views).Take(15).ToListAsync(ct))
            .Select(p => new AnalyticsPageDto(p.Path, p.Views, p.Visitors)).ToList();
        var devices = (await views.GroupBy(v => v.Device).Select(g => new { g.Key, Views = g.Count() }).ToListAsync(ct))
            .OrderByDescending(x => x.Views).Select(x => new AnalyticsShareDto(x.Key, x.Views)).ToList();
        var roles = (await views.GroupBy(v => v.Role ?? "Visitor").Select(g => new { g.Key, Views = g.Count() }).ToListAsync(ct))
            .OrderByDescending(x => x.Views).Select(x => new AnalyticsShareDto(x.Key, x.Views)).ToList();

        // ---- student activity in the same window ----
        var studentViews = views.Where(v => v.Role == "Student" && v.UserId != null);
        var activeStudents = await studentViews.Select(v => v.UserId).Distinct().CountAsync(ct);
        var lessonsCompleted = await db.LessonProgress.AsNoTracking().CountAsync(p => p.CompletedAt >= from, ct);
        var quizSubmissions = await db.Submissions.AsNoTracking().CountAsync(s => s.SubmittedAt >= from, ct);
        var arabicQuizzes = await db.ArabicProgress.AsNoTracking().CountAsync(p => p.UpdatedAt >= from, ct);
        var newEnrollments = await db.Enrollments.AsNoTracking().CountAsync(e => e.EnrolledAt >= from, ct);

        var top = await studentViews.GroupBy(v => v.UserId!.Value)
            .Select(g => new { Id = g.Key, Views = g.Count(), Last = g.Max(v => v.CreatedAt) })
            .OrderByDescending(x => x.Views).Take(10).ToListAsync(ct);
        var ids = top.Select(x => x.Id).ToList();
        var people = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, Name = u.FirstName + " " + u.LastName, u.Email }).ToDictionaryAsync(u => u.Id, ct);
        var lessons = await db.LessonProgress.AsNoTracking().Where(p => ids.Contains(p.UserId) && p.CompletedAt >= from)
            .GroupBy(p => p.UserId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var topStudents = top.Where(x => people.ContainsKey(x.Id)).Select(x => new AnalyticsStudentDto(
            x.Id, people[x.Id].Name, people[x.Id].Email, x.Views, lessons.GetValueOrDefault(x.Id), x.Last)).ToList();

        return new AdminAnalyticsDto(request.Days, totals?.Views ?? 0, totals?.Visitors ?? 0, totals?.Users ?? 0,
            daily, topPages, devices, roles,
            activeStudents, lessonsCompleted, quizSubmissions, arabicQuizzes, newEnrollments, topStudents);
    }
}
