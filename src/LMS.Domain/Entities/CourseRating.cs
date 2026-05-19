namespace LMS.Domain.Entities;

public class CourseRating
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }

    // 1..5 stars. Enforced by EF check constraint + FluentValidation.
    public int Rating { get; set; }

    // Optional written review. nvarchar(2000); empty string means star-only.
    public string Review { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Course Course { get; set; } = null!;
    public User Student { get; set; } = null!;
}
