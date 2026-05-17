namespace LMS.Domain.Entities;

/// <summary>
/// Many-to-many join: additional teachers who can author / grade alongside
/// the primary <see cref="Course.TeacherId"/>. Authorization treats primary
/// and co-instructors identically for most operations; primary-only actions
/// (delete course, manage co-instructors themselves, transfer) remain gated
/// to the primary teacher.
/// </summary>
public class CourseCoInstructor
{
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    public DateTime AddedAt { get; set; }
    public Guid AddedByUserId { get; set; }

    public Course Course { get; set; } = null!;
    public User User { get; set; } = null!;
}
