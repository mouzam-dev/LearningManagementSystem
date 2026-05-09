namespace LMS.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid AssessmentId { get; set; }
    public string Answers { get; set; } = string.Empty; // JSON: { questionId: answer, ... }
    public int? Score { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? GradedAt { get; set; }
    public string? Feedback { get; set; }

    public User Student { get; set; } = null!;
    public Assessment Assessment { get; set; } = null!;
}
