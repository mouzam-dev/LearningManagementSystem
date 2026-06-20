using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

/// <summary>
/// One student's mark within an <see cref="AttendanceSession"/>. Exactly one record
/// per (session, student); the unique index is the DB-side safety net.
///
/// CourseId / BranchId / SessionDate are denormalized from the parent session so
/// the hot reporting paths — a student's own attendance %, and an OrgAdmin's
/// branch-wide rollups — are index scans rather than multi-table joins.
/// </summary>
public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid AttendanceSessionId { get; set; }
    public Guid StudentId { get; set; }

    // Denormalized from the parent session for fast filtering/aggregation.
    public Guid CourseId { get; set; }
    public Guid? BranchId { get; set; }
    public DateOnly SessionDate { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    // Optional richer detail captured at marking time.
    public TimeOnly? CheckInTime { get; set; }
    public int? MinutesLate { get; set; }
    public string? Remark { get; set; }

    public Guid MarkedByTeacherId { get; set; }
    public DateTime MarkedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public AttendanceSession Session { get; set; } = null!;
    public User Student { get; set; } = null!;
}
