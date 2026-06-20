namespace LMS.Domain.Enums;

/// <summary>
/// Lifecycle of a class attendance session. A session is created Open (the teacher
/// can mark and re-mark students), then Finalized to lock it. Editing a finalized
/// session requires an explicit re-open (OrgAdmin override), which is audited.
/// Cancelled marks a class that did not take place; its records are excluded from
/// attendance percentages.
/// </summary>
public enum AttendanceSessionStatus
{
    /// <summary>Editable. The teacher is still marking / can revise.</summary>
    Open = 0,

    /// <summary>Locked. Records are read-only unless an admin re-opens the session.</summary>
    Finalized = 1,

    /// <summary>Class did not happen. Records (if any) are ignored in percentages.</summary>
    Cancelled = 2,
}
