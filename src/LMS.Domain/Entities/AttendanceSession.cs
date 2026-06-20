using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

/// <summary>
/// One attendance-taking event for a course: a class meeting on a given date and
/// time-slot. The roster of <see cref="AttendanceRecord"/> children is snapshotted
/// from the course's enrollments when the session is created, so later enrolment
/// changes never rewrite history.
///
/// Tenancy (OrganizationId/BranchId) is denormalized from the owning Course at
/// creation — mirroring how Course already denormalizes it from its Teacher — so
/// OrgAdmin branch-wise reports can scope without joining through Course/Users.
/// </summary>
public class AttendanceSession
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }

    // Denormalized tenancy, stamped from Course at create time.
    public Guid? OrganizationId { get; set; }
    public Guid? BranchId { get; set; }

    // The teacher (or co-instructor) who created/owns this session.
    public Guid TakenByTeacherId { get; set; }

    // Set when this attendance session was auto-created by a live class (links to
    // LiveSessions.Id). Null for sessions a teacher took manually. Plain column —
    // no navigation — to keep the modules loosely coupled.
    public Guid? LiveSessionId { get; set; }

    public DateOnly SessionDate { get; set; }

    // Supports multiple sessions per course per day (e.g. morning/evening batches).
    // Unique together with (CourseId, SessionDate). Defaults to 1 for a single
    // daily session.
    public int Slot { get; set; } = 1;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    // Optional topic / what was covered in this class.
    public string? Topic { get; set; }

    public AttendanceSessionStatus Status { get; set; } = AttendanceSessionStatus.Open;
    public DateTime? FinalizedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Course Course { get; set; } = null!;
    public User TakenByTeacher { get; set; } = null!;
    public Organization? Organization { get; set; }
    public Branch? Branch { get; set; }
    public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
}
