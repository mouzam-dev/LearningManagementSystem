using LMS.Application.Attendance.Common;
using LMS.Application.Attendance.Dtos;
using LMS.Application.Common;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Attendance.Queries;

// --------------------- The calling student's own attendance ---------------------

public record GetMyAttendanceQuery(Guid? CourseId, DateOnly? FromDate, DateOnly? ToDate)
    : IRequest<MyAttendanceDto>;

public class GetMyAttendanceQueryHandler : IRequestHandler<GetMyAttendanceQuery, MyAttendanceDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyAttendanceQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MyAttendanceDto> Handle(GetMyAttendanceQuery request, CancellationToken ct)
    {
        var studentId = _currentUser.GetUserId();

        var q = _db.AttendanceRecords.AsNoTracking()
            .Where(r => r.StudentId == studentId && r.Session.Status != AttendanceSessionStatus.Cancelled);
        if (request.CourseId is not null) q = q.Where(r => r.CourseId == request.CourseId);
        if (request.FromDate is not null) q = q.Where(r => r.SessionDate >= request.FromDate.Value);
        if (request.ToDate is not null) q = q.Where(r => r.SessionDate <= request.ToDate.Value);

        var rows = await q
            .Select(r => new
            {
                r.CourseId,
                CourseTitle = r.Session.Course.Title,
                r.SessionDate,
                Slot = r.Session.Slot,
                r.Status,
                r.Remark,
                r.AttendanceSessionId,
            })
            .ToListAsync(ct);

        var courses = rows
            .GroupBy(r => new { r.CourseId, r.CourseTitle })
            .Select(g =>
            {
                var statuses = g.Select(x => x.Status).ToList();
                return new MyCourseAttendanceDto
                {
                    CourseId = g.Key.CourseId,
                    CourseTitle = g.Key.CourseTitle,
                    Present = statuses.Count(s => s is AttendanceStatus.Present or AttendanceStatus.Remote),
                    Late = statuses.Count(s => s is AttendanceStatus.Late or AttendanceStatus.LeftEarly),
                    Absent = statuses.Count(s => s == AttendanceStatus.Absent),
                    Excused = statuses.Count(s => s == AttendanceStatus.Excused),
                    TotalCounted = statuses.Count(AttendanceWeights.CountsTowardTotal),
                    Percent = AttendanceWeights.Percent(statuses),
                };
            })
            .OrderBy(c => c.CourseTitle)
            .ToList();

        var recent = rows
            .OrderByDescending(r => r.SessionDate).ThenByDescending(r => r.Slot)
            .Take(50)
            .Select(r => new MyAttendanceMarkDto
            {
                CourseId = r.CourseId,
                CourseTitle = r.CourseTitle,
                SessionDate = r.SessionDate,
                Slot = r.Slot,
                Status = r.Status.ToString(),
                Remark = r.Remark,
            })
            .ToList();

        return new MyAttendanceDto
        {
            OverallPercent = AttendanceWeights.Percent(rows.Select(r => r.Status)),
            TotalSessions = rows.Select(r => r.AttendanceSessionId).Distinct().Count(),
            Courses = courses,
            Recent = recent,
        };
    }
}
