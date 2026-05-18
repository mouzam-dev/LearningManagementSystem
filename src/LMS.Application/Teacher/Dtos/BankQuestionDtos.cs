namespace LMS.Application.Teacher.Dtos;

/// <summary>Compact row for the bank list view (no full Options array).</summary>
public class BankQuestionListItemDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Points { get; set; }
    public string? Tag { get; set; }
    public int OptionCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Full payload for the bank editor (includes Options + CorrectAnswer).</summary>
public class BankQuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public IReadOnlyList<string>? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public int Points { get; set; }
    public string? Tag { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
