using LMS.Application.Common;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.LiveClasses.Common;

/// <summary>
/// Bridges live classes into the attendance module. When a live session starts, an
/// <see cref="AttendanceSession"/> is created for the course (linked via
/// <c>LiveSessionId</c>) with every enrolled student defaulted to Absent; as students
/// join, they're flipped to Present. The slot is the next free one for that course/
/// date so it never collides with a manually-taken session.
///
/// Callers add to the context here and SaveChanges themselves.
/// </summary>
internal static class LiveAttendanceLink
{
    public static async Task<AttendanceSession> EnsureSessionAsync(
        IApplicationDbContext db, LiveSession live, CancellationToken ct)
    {
        var existing = await db.AttendanceSessions
            .Include(s => s.Records)
            .FirstOrDefaultAsync(s => s.LiveSessionId == live.Id, ct);
        if (existing is not null) return existing;

        var now = DateTime.UtcNow;
        var date = DateOnly.FromDateTime(live.ScheduledStart);

        var maxSlot = await db.AttendanceSessions
            .Where(s => s.CourseId == live.CourseId && s.SessionDate == date)
            .Select(s => (int?)s.Slot)
            .MaxAsync(ct) ?? 0;

        var session = new AttendanceSession
        {
            Id = Guid.NewGuid(),
            CourseId = live.CourseId,
            OrganizationId = live.OrganizationId,
            BranchId = live.BranchId,
            TakenByTeacherId = live.HostTeacherId,
            LiveSessionId = live.Id,
            SessionDate = date,
            Slot = maxSlot + 1,
            Topic = $"Live class: {live.Title}",
            Status = AttendanceSessionStatus.Open,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var enrolledIds = await db.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == live.CourseId)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        foreach (var sid in enrolledIds)
        {
            session.Records.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                AttendanceSessionId = session.Id,
                StudentId = sid,
                CourseId = live.CourseId,
                BranchId = live.BranchId,
                SessionDate = date,
                Status = AttendanceStatus.Absent,
                MarkedByTeacherId = live.HostTeacherId,
                MarkedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        db.AttendanceSessions.Add(session);
        return session;
    }

    public static void MarkPresent(AttendanceSession session, Guid studentId, LiveSession live)
    {
        var now = DateTime.UtcNow;
        var rec = session.Records.FirstOrDefault(r => r.StudentId == studentId);
        if (rec is null)
        {
            // Enrolled after the snapshot — add them as Present.
            session.Records.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                AttendanceSessionId = session.Id,
                StudentId = studentId,
                CourseId = live.CourseId,
                BranchId = live.BranchId,
                SessionDate = DateOnly.FromDateTime(live.ScheduledStart),
                Status = AttendanceStatus.Present,
                MarkedByTeacherId = live.HostTeacherId,
                MarkedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else if (rec.Status == AttendanceStatus.Absent)
        {
            rec.Status = AttendanceStatus.Present;
            rec.MarkedAt = now;
            rec.UpdatedAt = now;
        }
    }
}
