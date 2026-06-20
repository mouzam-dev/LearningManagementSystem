using LMS.Application.Common;
using LMS.Application.LiveClasses.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.LiveClasses.Common;

/// <summary>
/// Shared projection of a <c>LiveSession</c> into <see cref="LiveSessionDto"/>.
/// The status enum is turned into its string name in memory (after materialization)
/// to avoid relying on SQL translation of <c>Enum.ToString()</c>.
/// </summary>
internal static class LiveClassMapping
{
    public static async Task<LiveSessionDto> LoadDtoAsync(IApplicationDbContext db, Guid id, CancellationToken ct)
    {
        var x = await db.LiveSessions.AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new
            {
                l.Id,
                l.CourseId,
                CourseTitle = l.Course.Title,
                l.BranchId,
                BranchName = l.Branch != null ? l.Branch.Name : null,
                HostFirst = l.HostTeacher.FirstName,
                HostLast = l.HostTeacher.LastName,
                l.Title,
                l.ScheduledStart,
                l.DurationMinutes,
                l.Status,
                l.Provider,
                l.RoomName,
                l.StartedAt,
                l.EndedAt,
                EnrolledCount = db.Enrollments.Count(e => e.CourseId == l.CourseId),
            })
            .FirstAsync(ct);

        return new LiveSessionDto
        {
            Id = x.Id,
            CourseId = x.CourseId,
            CourseTitle = x.CourseTitle,
            BranchId = x.BranchId,
            BranchName = x.BranchName,
            HostTeacherName = (x.HostFirst + " " + x.HostLast).Trim(),
            Title = x.Title,
            ScheduledStart = x.ScheduledStart,
            DurationMinutes = x.DurationMinutes,
            Status = x.Status.ToString(),
            Provider = x.Provider,
            RoomName = x.RoomName,
            StartedAt = x.StartedAt,
            EndedAt = x.EndedAt,
            EnrolledCount = x.EnrolledCount,
        };
    }
}
