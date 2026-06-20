using LMS.Application.Attendance.Common;
using LMS.Application.Attendance.Dtos;
using LMS.Application.Common;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Attendance.Queries;

// --------------------- List a course's sessions (newest first) ---------------------

public record GetCourseAttendanceSessionsQuery(Guid CourseId, int Page = 1, int PageSize = 30)
    : IRequest<IReadOnlyList<AttendanceSessionDto>>;

public class GetCourseAttendanceSessionsQueryHandler
    : IRequestHandler<GetCourseAttendanceSessionsQuery, IReadOnlyList<AttendanceSessionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseAttendanceSessionsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AttendanceSessionDto>> Handle(
        GetCourseAttendanceSessionsQuery request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();
        await AttendanceAuthorization.RequireTeacherCourseAsync(_db, request.CourseId, teacherId, ct);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var sessions = await _db.AttendanceSessions.AsNoTracking()
            .Where(s => s.CourseId == request.CourseId)
            .OrderByDescending(s => s.SessionDate).ThenByDescending(s => s.Slot)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.CourseId,
                CourseTitle = s.Course.Title,
                s.BranchId,
                BranchName = s.Branch != null ? s.Branch.Name : null,
                s.SessionDate,
                s.Slot,
                s.StartTime,
                s.EndTime,
                s.Topic,
                s.Status,
                s.CreatedAt,
            })
            .ToListAsync(ct);

        var ids = sessions.Select(s => s.Id).ToList();
        var recStatuses = await _db.AttendanceRecords.AsNoTracking()
            .Where(r => ids.Contains(r.AttendanceSessionId))
            .Select(r => new { r.AttendanceSessionId, r.Status })
            .ToListAsync(ct);

        var bySession = recStatuses
            .GroupBy(r => r.AttendanceSessionId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Status).ToList());

        return sessions.Select(s =>
        {
            var dto = new AttendanceSessionDto
            {
                Id = s.Id,
                CourseId = s.CourseId,
                CourseTitle = s.CourseTitle,
                BranchId = s.BranchId,
                BranchName = s.BranchName,
                SessionDate = s.SessionDate,
                Slot = s.Slot,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Topic = s.Topic,
                Status = s.Status.ToString(),
                CreatedAt = s.CreatedAt,
            };
            AttendanceMapping.FillCounts(
                dto, bySession.TryGetValue(s.Id, out var st) ? st : new List<AttendanceStatus>());
            return dto;
        }).ToList();
    }
}

// --------------------- One session's roster (the marking grid) ---------------------

public record GetAttendanceSessionRosterQuery(Guid SessionId) : IRequest<SessionRosterDto>;

public class GetAttendanceSessionRosterQueryHandler
    : IRequestHandler<GetAttendanceSessionRosterQuery, SessionRosterDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAttendanceSessionRosterQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SessionRosterDto> Handle(GetAttendanceSessionRosterQuery request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        var courseId = await _db.AttendanceSessions.AsNoTracking()
            .Where(s => s.Id == request.SessionId)
            .Select(s => (Guid?)s.CourseId)
            .FirstOrDefaultAsync(ct);

        if (courseId is null)
        {
            throw new KeyNotFoundException($"Attendance session {request.SessionId} was not found.");
        }

        await AttendanceAuthorization.RequireTeacherCourseAsync(_db, courseId.Value, teacherId, ct);
        return await AttendanceMapping.LoadRosterAsync(_db, request.SessionId, ct);
    }
}

// --------------------- Per-student attendance % across a course ---------------------

public record GetCourseAttendanceSummaryQuery(Guid CourseId) : IRequest<CourseAttendanceSummaryDto>;

public class GetCourseAttendanceSummaryQueryHandler
    : IRequestHandler<GetCourseAttendanceSummaryQuery, CourseAttendanceSummaryDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseAttendanceSummaryQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CourseAttendanceSummaryDto> Handle(GetCourseAttendanceSummaryQuery request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();
        var course = await AttendanceAuthorization.RequireTeacherCourseAsync(_db, request.CourseId, teacherId, ct);

        var sessionCount = await _db.AttendanceSessions.AsNoTracking()
            .CountAsync(s => s.CourseId == request.CourseId && s.Status != AttendanceSessionStatus.Cancelled, ct);

        var rows = await _db.AttendanceRecords.AsNoTracking()
            .Where(r => r.CourseId == request.CourseId && r.Session.Status != AttendanceSessionStatus.Cancelled)
            .Select(r => new
            {
                r.StudentId,
                First = r.Student.FirstName,
                Last = r.Student.LastName,
                Avatar = r.Student.ProfilePictureUrl,
                r.Status,
            })
            .ToListAsync(ct);

        var students = rows
            .GroupBy(r => new { r.StudentId, r.First, r.Last, r.Avatar })
            .Select(g =>
            {
                var statuses = g.Select(x => x.Status).ToList();
                return new StudentAttendanceRowDto
                {
                    StudentId = g.Key.StudentId,
                    StudentName = (g.Key.First + " " + g.Key.Last).Trim(),
                    StudentAvatarUrl = g.Key.Avatar,
                    Present = statuses.Count(s => s == AttendanceStatus.Present),
                    Remote = statuses.Count(s => s == AttendanceStatus.Remote),
                    Late = statuses.Count(s => s == AttendanceStatus.Late),
                    LeftEarly = statuses.Count(s => s == AttendanceStatus.LeftEarly),
                    Absent = statuses.Count(s => s == AttendanceStatus.Absent),
                    Excused = statuses.Count(s => s == AttendanceStatus.Excused),
                    TotalCounted = statuses.Count(AttendanceWeights.CountsTowardTotal),
                    Percent = AttendanceWeights.Percent(statuses),
                };
            })
            .OrderBy(x => x.StudentName)
            .ToList();

        return new CourseAttendanceSummaryDto
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            SessionCount = sessionCount,
            OverallPercent = AttendanceWeights.Percent(rows.Select(r => r.Status)),
            Students = students,
        };
    }
}
