namespace LMS.Domain.Entities;

/// <summary>
/// A learner's best quiz score for one lesson of an Arabic unit.
/// <see cref="LessonNumber"/> 0 means the unit test (questions drawn from all lessons).
/// </summary>
public class ArabicProgress
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string UnitId { get; set; } = null!;
    public int LessonNumber { get; set; }
    public int BestScore { get; set; }   // percent 0-100
    public int Attempts { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ArabicUnit Unit { get; set; } = null!;
}
