namespace LMS.Domain.Enums;

/// <summary>
/// How a single student was marked for one class session. Richer than a plain
/// present/absent flag so reports can distinguish lateness, sanctioned absence,
/// and remote attendance. Stored as a string in the database (see
/// AttendanceRecordConfiguration) for human-readable rows and stable meaning if
/// the enum is reordered.
/// </summary>
public enum AttendanceStatus
{
    /// <summary>In class, on time. Counts as a full attendance (weight 1.0).</summary>
    Present = 0,

    /// <summary>Did not attend, unexcused. Weight 0.0 and counted in the denominator.</summary>
    Absent = 1,

    /// <summary>Attended but arrived late. Weight 0.5 by default.</summary>
    Late = 2,

    /// <summary>Sanctioned absence (approved leave, medical, etc.). Excluded from the percentage denominator.</summary>
    Excused = 3,

    /// <summary>Attended remotely / online. Counts as a full attendance (weight 1.0).</summary>
    Remote = 4,

    /// <summary>Attended but left before the session ended. Weight 0.5 by default.</summary>
    LeftEarly = 5,
}
