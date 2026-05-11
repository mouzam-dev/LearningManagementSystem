namespace LMS.Application.Student.Dtos;

public class DeadlineDto
{
    public Guid AssessmentId { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;
    /// <summary>"Quiz" or "Assignment".</summary>
    public string Type { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
}
