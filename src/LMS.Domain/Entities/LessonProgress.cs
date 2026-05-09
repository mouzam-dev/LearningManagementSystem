namespace LMS.Domain.Entities;

public class LessonProgress
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int WatchTimeSeconds { get; set; }

    public User User { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}
