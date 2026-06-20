namespace LMS.Application.LiveClasses.Dtos;

public class LiveSessionDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string HostTeacherName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledStart { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty; // LiveSessionStatus name
    public string Provider { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int EnrolledCount { get; set; }
}

/// <summary>Returned to a student when they join — everything the front-end needs to embed the room.</summary>
public class LiveJoinInfoDto
{
    public Guid LiveSessionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
