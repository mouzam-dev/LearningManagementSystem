namespace LMS.Application.Student.Dtos;

public class CourseListItemDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int EnrolledCount { get; set; }
    public int? MaxStudents { get; set; }
    /// <summary>True if the calling student already has an enrollment for this course.</summary>
    public bool IsEnrolled { get; set; }
}
