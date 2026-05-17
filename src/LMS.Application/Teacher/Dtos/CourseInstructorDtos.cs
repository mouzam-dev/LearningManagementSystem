namespace LMS.Application.Teacher.Dtos;

public class CourseInstructorDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string? BranchName { get; set; }
    /// <summary>True when this row is the original course owner (Course.TeacherId).</summary>
    public bool IsPrimary { get; set; }
    /// <summary>When the co-instructor was added — null for the primary teacher.</summary>
    public DateTime? AddedAt { get; set; }
}

/// <summary>Eligible teacher to add as a co-instructor — same org as the course's
/// primary teacher, not already an instructor, active.</summary>
public class EligibleCoInstructorDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? BranchName { get; set; }
}
