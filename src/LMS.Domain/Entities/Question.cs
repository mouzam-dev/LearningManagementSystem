namespace LMS.Domain.Entities;

public class Question
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "MCQ" | "TrueFalse" | "ShortAnswer" | "Essay"
    public string? Options { get; set; }              // JSON array for MCQ
    public string? CorrectAnswer { get; set; }
    public int Points { get; set; } = 1;
    public int Order { get; set; }

    public Assessment Assessment { get; set; } = null!;
}
