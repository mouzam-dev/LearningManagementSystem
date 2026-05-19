namespace LMS.Domain.Entities;

public class TeacherRating
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }

    // The course the student took with this teacher — captured so the rating
    // is anchored to a concrete experience (a student may only rate teachers
    // for courses they're enrolled in).
    public Guid CourseId { get; set; }

    public int Rating { get; set; }
    public string Review { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Teacher { get; set; } = null!;
    public User Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
