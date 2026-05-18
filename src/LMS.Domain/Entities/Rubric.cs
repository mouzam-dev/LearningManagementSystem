namespace LMS.Domain.Entities;

/// <summary>
/// Structured grading template (BRD TCH-025). A teacher writes a rubric once
/// then attaches it to one or more Assignment-type assessments. When a
/// submission is graded, the teacher scores each criterion individually and
/// the per-criterion breakdown is persisted as JSON on the submission.
///
/// Private per teacher today (same pattern as <see cref="BankQuestion"/>).
/// </summary>
public class Rubric
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Teacher { get; set; } = null!;
    public ICollection<RubricCriterion> Criteria { get; set; } = new List<RubricCriterion>();
}
