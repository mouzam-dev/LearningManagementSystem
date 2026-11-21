namespace LMS.Domain.Entities;

/// <summary>
/// Multiple-choice practice question. <see cref="OptionsAr"/> is an optional
/// Arabic-only rendering of <see cref="Options"/> (same order, same answer index).
/// </summary>
public class ArabicQuizQuestion
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public int Order { get; set; }
    public string QuestionAr { get; set; } = string.Empty;
    public string QuestionEn { get; set; } = string.Empty;
    public string QuestionUr { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public List<string>? OptionsAr { get; set; }
    public int AnswerIndex { get; set; }

    public ArabicLesson Lesson { get; set; } = null!;
}
