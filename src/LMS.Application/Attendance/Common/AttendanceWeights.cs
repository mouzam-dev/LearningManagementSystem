using LMS.Domain.Enums;

namespace LMS.Application.Attendance.Common;

/// <summary>
/// Single source of truth for how an <see cref="AttendanceStatus"/> contributes to
/// an attendance percentage. Present/Remote = full credit, Late/LeftEarly = half,
/// Absent = none, Excused = excluded from the denominator entirely.
/// </summary>
public static class AttendanceWeights
{
    public static decimal Weight(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => 1.0m,
        AttendanceStatus.Remote => 1.0m,
        AttendanceStatus.Late => 0.5m,
        AttendanceStatus.LeftEarly => 0.5m,
        _ => 0.0m, // Absent, Excused
    };

    /// <summary>Excused records count as neither present nor absent.</summary>
    public static bool CountsTowardTotal(AttendanceStatus status) =>
        status != AttendanceStatus.Excused;

    /// <summary>
    /// Attendance percentage (0–100, one decimal) for a set of marks. Excused marks
    /// are dropped from the base. Returns 0 when nothing counts.
    /// </summary>
    public static decimal Percent(IEnumerable<AttendanceStatus> statuses)
    {
        decimal total = 0, earned = 0;
        foreach (var s in statuses)
        {
            if (!CountsTowardTotal(s)) continue;
            total += 1;
            earned += Weight(s);
        }
        return total == 0 ? 0 : Math.Round(earned / total * 100, 1);
    }
}
