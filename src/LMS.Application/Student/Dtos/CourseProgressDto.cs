namespace LMS.Application.Student.Dtos;

public class CourseProgressDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    /// <summary>Per-enrollment progress, 0-100.</summary>
    public decimal ProgressPercentage { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime EnrolledAt { get; set; }
}
