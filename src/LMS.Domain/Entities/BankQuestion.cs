namespace LMS.Domain.Entities;

/// <summary>
/// A reusable question in a teacher's private question bank (BRD TCH-021).
/// Same shape as <see cref="Question"/> but owned by a teacher rather than an
/// assessment. Importing into an assessment COPIES the row into a new Question
/// (no live reference), so editing a bank question never silently changes a
/// past assessment.
/// </summary>
public class BankQuestion
{
    public Guid Id { get; set; }

    /// <summary>Owner of the bank entry. Bank is private per teacher today.</summary>
    public Guid TeacherId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    /// <summary>"MCQ" | "TrueFalse" | "ShortAnswer" | "Essay" — same set as Question.Type.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>JSON array of options for MCQ questions; null for other types.</summary>
    public string? Options { get; set; }

    public string? CorrectAnswer { get; set; }

    public int Points { get; set; } = 1;

    /// <summary>Free-text tag for grouping (e.g. "Algebra", "Unit 3"). Optional.</summary>
    public string? Tag { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Teacher { get; set; } = null!;
}
