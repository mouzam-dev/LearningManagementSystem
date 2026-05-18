namespace LMS.Domain.Entities;

public class RubricCriterion
{
    public Guid Id { get; set; }
    public Guid RubricId { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>What "full marks" looks like for this criterion. Optional.</summary>
    public string? Description { get; set; }
    /// <summary>Maximum points awardable for this criterion (1-100).</summary>
    public int MaxPoints { get; set; }
    public int Order { get; set; }

    public Rubric Rubric { get; set; } = null!;
}
