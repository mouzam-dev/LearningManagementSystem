using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class HadithConfiguration : IEntityTypeConfiguration<Hadith>
{
    public void Configure(EntityTypeBuilder<Hadith> e)
    {
        e.ToTable("Hadiths");

        e.HasKey(x => x.Id);

        e.Property(x => x.Collection).HasMaxLength(50).IsRequired();
        e.Property(x => x.BookNumber).HasMaxLength(20).IsRequired();
        e.Property(x => x.ChapterId).HasColumnType("decimal(6,2)");
        e.Property(x => x.HadithNumber).HasMaxLength(50).IsRequired();

        // Grades are short in practice but the source column allows up to 2000.
        e.Property(x => x.GradeEn).HasMaxLength(2000);
        e.Property(x => x.GradeAr).HasMaxLength(2000);
        e.Property(x => x.GradeCategory).HasMaxLength(20);
        e.Property(x => x.BookNameEn).HasMaxLength(300);
        e.Property(x => x.BookNameAr).HasMaxLength(300);
        // ChapterEn/Ar and BodyEn/Ar are left as nvarchar(max) — chapter names and
        // narrations (with raw markup) vary widely and must not truncate on import.

        // Book page query + canonical ordering within the book.
        e.HasIndex(x => new { x.Collection, x.BookNumber, x.OurHadithNumber });
        // Collection-level counts (totalHadith, random pick).
        e.HasIndex(x => x.Collection);
        // Grade filter (advanced search).
        e.HasIndex(x => new { x.Collection, x.GradeCategory });
    }
}
