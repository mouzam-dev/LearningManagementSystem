using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

/// <summary>
/// A scheduled live online class for a course, held in an embedded video room
/// (Jitsi for v1). The teacher schedules it, Starts it (→ Live), and Ends it;
/// enrolled students join the room while it is Live. Tenancy is denormalized from
/// the owning course, mirroring <see cref="AttendanceSession"/>.
///
/// When a student joins a Live session, the join handler marks them Present in a
/// linked <see cref="AttendanceSession"/> (AttendanceSession.LiveSessionId), tying
/// live classes into the attendance module.
/// </summary>
public class LiveSession
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }

    // Denormalized tenancy, stamped from Course at create time.
    public Guid? OrganizationId { get; set; }
    public Guid? BranchId { get; set; }

    public Guid HostTeacherId { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; } // UTC
    public int DurationMinutes { get; set; } = 60;

    public LiveSessionStatus Status { get; set; } = LiveSessionStatus.Scheduled;

    // Video provider + room. RoomName is GUID-based and unguessable, so the public
    // Jitsi server doesn't expose the class to random users.
    public string Provider { get; set; } = "Jitsi";
    public string RoomName { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Course Course { get; set; } = null!;
    public User HostTeacher { get; set; } = null!;
    public Organization? Organization { get; set; }
    public Branch? Branch { get; set; }
}
