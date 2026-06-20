using LMS.Application.Attendance.Dtos;
using LMS.Application.Common;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Attendance.Common;

/// <summary>
/// Shared projection helpers so the create/mark/lock handlers and the roster query
/// all return an identically-shaped <see cref="SessionRosterDto"/>.
/// Status enums are turned into their string names in memory (after materialization)
/// to avoid relying on SQL translation of <c>Enum.ToString()</c>.
/// </summary>
internal static class AttendanceMapping
{
    public static void FillCounts(AttendanceSessionDto dto, IEnumerable<AttendanceStatus> statuses)
    {
        var list = statuses as IReadOnlyList<AttendanceStatus> ?? statuses.ToList();
        dto.StudentCount = list.Count;
        dto.PresentCount = list.Count(s => s is AttendanceStatus.Present or AttendanceStatus.Remote);
        dto.LateCount = list.Count(s => s is AttendanceStatus.Late or AttendanceStatus.LeftEarly);
        dto.AbsentCount = list.Count(s => s == AttendanceStatus.Absent);
        dto.ExcusedCount = list.Count(s => s == AttendanceStatus.Excused);
        dto.AttendancePercent = AttendanceWeights.Percent(list);
    }

    public static async Task<AttendanceSessionDto> LoadSessionDtoAsync(
        IApplicationDbContext db, Guid sessionId, CancellationToken ct)
    {
        var s = await db.AttendanceSessions.AsNoTracking()
            .Where(x => x.Id == sessionId)
            .Select(x => new
            {
                x.Id,
                x.CourseId,
                CourseTitle = x.Course.Title,
                x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : null,
                x.SessionDate,
                x.Slot,
                x.StartTime,
                x.EndTime,
                x.Topic,
                x.Status,
                x.CreatedAt,
            })
            .FirstAsync(ct);

        var statuses = await db.AttendanceRecords.AsNoTracking()
            .Where(r => r.AttendanceSessionId == sessionId)
            .Select(r => r.Status)
            .ToListAsync(ct);

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
        FillCounts(dto, statuses);
        return dto;
    }

    public static async Task<SessionRosterDto> LoadRosterAsync(
        IApplicationDbContext db, Guid sessionId, CancellationToken ct)
    {
        var session = await LoadSessionDtoAsync(db, sessionId, ct);

        var recs = await db.AttendanceRecords.AsNoTracking()
            .Where(r => r.AttendanceSessionId == sessionId)
            .OrderBy(r => r.Student.FirstName).ThenBy(r => r.Student.LastName)
            .Select(r => new
            {
                r.Id,
                r.StudentId,
                First = r.Student.FirstName,
                Last = r.Student.LastName,
                Avatar = r.Student.ProfilePictureUrl,
                r.Status,
                r.CheckInTime,
                r.MinutesLate,
                r.Remark,
            })
            .ToListAsync(ct);

        var records = recs.Select(r => new AttendanceRecordDto
        {
            Id = r.Id,
            StudentId = r.StudentId,
            StudentName = (r.First + " " + r.Last).Trim(),
            StudentAvatarUrl = r.Avatar,
            Status = r.Status.ToString(),
            CheckInTime = r.CheckInTime,
            MinutesLate = r.MinutesLate,
            Remark = r.Remark,
        }).ToList();

        return new SessionRosterDto { Session = session, Records = records };
    }
}
