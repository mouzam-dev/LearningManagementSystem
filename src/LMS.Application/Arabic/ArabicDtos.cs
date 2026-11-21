namespace LMS.Application.Arabic;

public record ArabicUnitSummaryDto(string Id, int UnitNumber, string TitleAr, string TitleEn, string TitleUr, int LessonCount);
public record ArabicBookDto(int Number, IReadOnlyList<ArabicUnitSummaryDto> Units);

public record ArabicLineDto(string Speaker, string Ar, string Tr, string En, string Ur);
public record ArabicWordDto(string Ar, string Tr, string En, string Ur);
public record ArabicQuizDto(string QuestionAr, string QuestionEn, string QuestionUr,
    IReadOnlyList<string> Options, IReadOnlyList<string>? OptionsAr, int AnswerIndex);

public record ArabicLessonDto(int Number, string TitleAr, string TitleEn, string TitleUr,
    string IntroAr, string IntroEn, string IntroUr,
    IReadOnlyList<ArabicLineDto> Dialogue, IReadOnlyList<ArabicWordDto> Vocab, IReadOnlyList<ArabicQuizDto> Quiz);

public record ArabicUnitDto(string Id, int BookNumber, int UnitNumber, string TitleAr, string TitleEn, string TitleUr,
    IReadOnlyList<ArabicLessonDto> Lessons);

/// <summary>A vocabulary word with where it was taught, for the word-cards page.</summary>
public record ArabicWordCardDto(string Ar, string Tr, string En, string Ur, string UnitId, int UnitNumber, int LessonNumber);

public record ArabicProgressDto(string UnitId, int LessonNumber, int BestScore, int Attempts, DateTime UpdatedAt);
