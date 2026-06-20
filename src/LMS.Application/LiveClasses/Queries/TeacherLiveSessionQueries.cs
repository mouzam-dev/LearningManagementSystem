using LMS.Application.Attendance.Common;
using LMS.Application.Common;
using LMS.Application.LiveClasses.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.LiveClasses.Queries;

/// <summary>A teacher's live sessions for one of their courses (newest first).</summary>
public record GetCourseLiveSessionsQuery(Guid CourseId) : IRequest<IReadOnlyList<LiveSessionDto>>;

public class GetCourseLiveSessionsQueryHandler
    : IRequestHandler<GetCourseLiveSessionsQuery, IReadOnlyList<LiveSessionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseLiveSessionsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<LiveSessionDto>> Handle(GetCourseLiveSessionsQuery request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();
        await AttendanceAuthorization.RequireTeacherCourseAsync(_db, request.CourseId, teacherId, ct);

        var sessions = await _db.LiveSessions.AsNoTracking()
            .Include(l => l.Course)
            .Include(l => l.Branch)
            .Include(l => l.HostTeacher)
            .Where(l => l.CourseId == request.CourseId)
            .OrderByDescending(l => l.ScheduledStart)
            .ToListAsync(ct);

        var enrolled = await _db.Enrollments.AsNoTracking()
            .CountAsync(e => e.CourseId == request.CourseId, ct);

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
            EnrolledCount = enrolled,
        }).ToList();
    }
}
