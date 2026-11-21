namespace LMS.Domain.Entities;

/// <summary>A lesson inside an <see cref="ArabicUnit"/>: intro text, dialogue, vocabulary and a quiz.</summary>
public class ArabicLesson
{
    public int Id { get; set; }
    public string UnitId { get; set; } = null!;
    public int Number { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleUr { get; set; } = string.Empty;
    public string IntroAr { get; set; } = string.Empty;
    public string IntroEn { get; set; } = string.Empty;
    public string IntroUr { get; set; } = string.Empty;

    public ArabicUnit Unit { get; set; } = null!;
    public ICollection<ArabicDialogueLine> DialogueLines { get; set; } = new List<ArabicDialogueLine>();
    public ICollection<ArabicVocabItem> VocabItems { get; set; } = new List<ArabicVocabItem>();
    public ICollection<ArabicQuizQuestion> QuizQuestions { get; set; } = new List<ArabicQuizQuestion>();
}
