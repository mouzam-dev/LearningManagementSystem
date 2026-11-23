using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

// Qur'an text (seeded) and the AI search usage log. The full-text "SearchVector" columns on
// QuranVerses and Hadiths are created by raw SQL in the AddLibrarySearch migration and are not
// mapped here: only the search service (raw SQL) reads them.

public class QuranSurahConfiguration : IEntityTypeConfiguration<QuranSurah>
{
    public void Configure(EntityTypeBuilder<QuranSurah> e)
    {
        e.ToTable("QuranSurahs");
        e.HasKey(x => x.Number);
        e.Property(x => x.Number).ValueGeneratedNever();
        e.Property(x => x.NameAr).HasMaxLength(100);
        e.Property(x => x.NameEn).HasMaxLength(100);
        e.Property(x => x.NameTransliterated).HasMaxLength(100);
        e.Property(x => x.RevelationPlace).HasMaxLength(20);
    }
}

public class QuranVerseConfiguration : IEntityTypeConfiguration<QuranVerse>
{
    public void Configure(EntityTypeBuilder<QuranVerse> e)
    {
        e.ToTable("QuranVerses");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.SurahNumber, x.VerseNumber }).IsUnique();
        e.HasOne(x => x.Surah).WithMany(s => s.Verses).HasForeignKey(x => x.SurahNumber).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AiQueryLogConfiguration : IEntityTypeConfiguration<AiQueryLog>
{
    public void Configure(EntityTypeBuilder<AiQueryLog> e)
    {
        e.ToTable("AiQueryLogs");
        e.HasKey(x => x.Id);
        e.Property(x => x.Mode).HasMaxLength(10);
        e.Property(x => x.Question).HasMaxLength(500);
        e.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
