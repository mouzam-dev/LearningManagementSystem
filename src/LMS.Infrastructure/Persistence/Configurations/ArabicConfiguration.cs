using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

// Arabic course module: Units -> Lessons -> (DialogueLines, VocabItems, QuizQuestions), plus per-user progress.
// Text columns are left unbounded: they are seeded reference content and must not truncate.

public class ArabicUnitConfiguration : IEntityTypeConfiguration<ArabicUnit>
{
    public void Configure(EntityTypeBuilder<ArabicUnit> e)
    {
        e.ToTable("ArabicUnits");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasMaxLength(10);
        e.HasIndex(x => new { x.BookNumber, x.UnitNumber }).IsUnique();
    }
}

public class ArabicLessonConfiguration : IEntityTypeConfiguration<ArabicLesson>
{
    public void Configure(EntityTypeBuilder<ArabicLesson> e)
    {
        e.ToTable("ArabicLessons");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.UnitId, x.Number }).IsUnique();
        e.HasOne(x => x.Unit).WithMany(u => u.Lessons).HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ArabicDialogueLineConfiguration : IEntityTypeConfiguration<ArabicDialogueLine>
{
    public void Configure(EntityTypeBuilder<ArabicDialogueLine> e)
    {
        e.ToTable("ArabicDialogueLines");
        e.HasKey(x => x.Id);
        e.Property(x => x.Speaker).HasMaxLength(100);
        e.HasIndex(x => new { x.LessonId, x.Order });
        e.HasOne(x => x.Lesson).WithMany(l => l.DialogueLines).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ArabicVocabItemConfiguration : IEntityTypeConfiguration<ArabicVocabItem>
{
    public void Configure(EntityTypeBuilder<ArabicVocabItem> e)
    {
        e.ToTable("ArabicVocabItems");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.LessonId, x.Order });
        e.HasOne(x => x.Lesson).WithMany(l => l.VocabItems).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ArabicQuizQuestionConfiguration : IEntityTypeConfiguration<ArabicQuizQuestion>
{
    public void Configure(EntityTypeBuilder<ArabicQuizQuestion> e)
    {
        e.ToTable("ArabicQuizQuestions");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.LessonId, x.Order });
        e.HasOne(x => x.Lesson).WithMany(l => l.QuizQuestions).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ArabicProgressConfiguration : IEntityTypeConfiguration<ArabicProgress>
{
    public void Configure(EntityTypeBuilder<ArabicProgress> e)
    {
        e.ToTable("ArabicProgress", t => t.HasCheckConstraint(
            "CK_ArabicProgress_BestScore_Range", "\"BestScore\" BETWEEN 0 AND 100"));
        e.HasKey(x => x.Id);
        // One row per (user, unit, lesson); the handler upserts, the index is the safety net.
        e.HasIndex(x => new { x.UserId, x.UnitId, x.LessonNumber }).IsUnique();
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Cascade);
    }
}
