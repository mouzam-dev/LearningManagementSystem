namespace LMS.Application.Student.Dtos;

public class DashboardDto
{
    public int TotalCourses { get; set; }
    public int CompletedCourses { get; set; }
    /// <summary>Average enrollment progress across all courses, 0-100.</summary>
    public decimal OverallProgress { get; set; }
    public IReadOnlyList<CourseProgressDto> EnrolledCourses { get; set; } = Array.Empty<CourseProgressDto>();
    public IReadOnlyList<DeadlineDto> UpcomingDeadlines { get; set; } = Array.Empty<DeadlineDto>();
}
