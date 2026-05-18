namespace LMS.Application.Teacher.Dtos;

/// <summary>Row on the rubrics list page.</summary>
public class RubricListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CriterionCount { get; set; }
    public int TotalPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Full rubric for the editor + the grader UI.</summary>
public class RubricDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalPoints { get; set; }
    public IReadOnlyList<RubricCriterionDto> Criteria { get; set; } = Array.Empty<RubricCriterionDto>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RubricCriterionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxPoints { get; set; }
    public int Order { get; set; }
}

/// <summary>
/// Upsert payload (create + replace-on-update). Criteria are sent in display
/// order; the server preserves that order. Existing criterion IDs are not
/// re-used on update — update wipes + reinserts, which keeps the API surface
/// small (no per-row partial updates) and is fine for a private bank.
/// </summary>
public class RubricCriterionInput
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MaxPoints { get; set; } = 10;
}
