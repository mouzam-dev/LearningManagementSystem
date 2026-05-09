using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> e)
    {
        e.ToTable("LessonProgress");
        e.HasKey(x => x.Id);

        e.HasOne(x => x.User)
            .WithMany(u => u.LessonProgress)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasOne(x => x.Lesson)
            .WithMany(l => l.Progress)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();
    }
}
