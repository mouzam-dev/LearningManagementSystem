namespace LMS.Domain.Entities;

public class Assessment
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Quiz" | "Assignment"
    public int? TimeLimit { get; set; }              // minutes
    public int PassingScore { get; set; }            // 0-100
    public DateTime? DueDate { get; set; }
    public int? MaxAttempts { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Marks this assessment as the course's final exam. At most one per course.
    /// Passing it (a graded submission at/above <see cref="PassingScore"/>) is what
    /// unlocks the student's Certificate of Completion.
    /// </summary>
    public bool IsFinalExam { get; set; }

    /// <summary>
    /// Optional grading rubric (BRD TCH-025). Only meaningful for Assignment-
    /// type assessments; quizzes auto-grade and ignore this. Nullable; null
    /// means single-score grading (the existing behavior).
    /// </summary>
    public Guid? RubricId { get; set; }

    public Course Course { get; set; } = null!;
    public Rubric? Rubric { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
