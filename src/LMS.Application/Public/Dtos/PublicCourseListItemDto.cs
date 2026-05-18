namespace LMS.Application.Public.Dtos;

/// <summary>
/// Course card shape served to anonymous visitors on the public homepage.
/// Mirrors <see cref="LMS.Application.Student.Dtos.CourseListItemDto"/> but omits
/// the per-user IsEnrolled flag — there is no caller identity for this endpoint.
/// </summary>
public class PublicCourseListItemDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int EnrolledCount { get; set; }
    public int? MaxStudents { get; set; }
}
