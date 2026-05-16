namespace LMS.Domain.Entities;

/// <summary>
/// A teacher-authored message broadcast to all students enrolled in a course.
/// When created, the application service fans the announcement out to every
/// enrolled student as an in-app <see cref="Notification"/> row.
/// </summary>
public class Announcement
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Course Course { get; set; } = null!;
    public User Author { get; set; } = null!;
}
