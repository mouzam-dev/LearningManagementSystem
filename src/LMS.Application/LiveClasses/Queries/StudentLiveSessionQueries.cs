using LMS.Application.Common;
using LMS.Application.LiveClasses.Dtos;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.LiveClasses.Queries;

/// <summary>
/// Upcoming + currently-live sessions for the courses the calling student is
/// enrolled in, soonest first. The UI enables "Join" only for ones that are Live.
/// </summary>
public record GetMyLiveSessionsQuery() : IRequest<IReadOnlyList<LiveSessionDto>>;

public class GetMyLiveSessionsQueryHandler
    : IRequestHandler<GetMyLiveSessionsQuery, IReadOnlyList<LiveSessionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyLiveSessionsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<LiveSessionDto>> Handle(GetMyLiveSessionsQuery request, CancellationToken ct)
    {
        var studentId = _currentUser.GetUserId();

        var sessions = await _db.LiveSessions.AsNoTracking()
            .Include(l => l.Course)
            .Include(l => l.Branch)
            .Include(l => l.HostTeacher)
            .Where(l => _db.Enrollments.Any(e => e.StudentId == studentId && e.CourseId == l.CourseId)
                        && (l.Status == LiveSessionStatus.Scheduled || l.Status == LiveSessionStatus.Live))
            .OrderBy(l => l.ScheduledStart)
            .ToListAsync(ct);

        return sessions.Select(l => new LiveSessionDto
        {
            Id = l.Id,
            CourseId = l.CourseId,
            CourseTitle = l.Course.Title,
            BranchId = l.BranchId,
            BranchName = l.Branch?.Name,
            HostTeacherName = (l.HostTeacher.FirstName + " " + l.HostTeacher.LastName).Trim(),
            Title = l.Title,
            ScheduledStart = l.ScheduledStart,
            DurationMinutes = l.DurationMinutes,
            Status = l.Status.ToString(),
            Provider = l.Provider,
            RoomName = l.RoomName,
            StartedAt = l.StartedAt,
            EndedAt = l.EndedAt,
            EnrolledCount = 0,
        }).ToList();
    }
}
