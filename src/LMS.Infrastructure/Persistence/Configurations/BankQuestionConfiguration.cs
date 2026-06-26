using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class BankQuestionConfiguration : IEntityTypeConfiguration<BankQuestion>
{
    public void Configure(EntityTypeBuilder<BankQuestion> e)
    {
        e.ToTable("BankQuestions");
        e.HasKey(x => x.Id);

        e.Property(x => x.QuestionText).IsRequired().HasColumnType("text");
        e.Property(x => x.Type).IsRequired().HasMaxLength(20);
        e.Property(x => x.Options).HasColumnType("text");
        e.Property(x => x.CorrectAnswer).HasColumnType("text");
        e.Property(x => x.Tag).HasMaxLength(80);

        e.HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        // Most queries filter by TeacherId; add Tag as a secondary key so the
        // tag-filter dropdown stays cheap as banks grow.
        e.HasIndex(x => new { x.TeacherId, x.Tag });
        e.HasIndex(x => new { x.TeacherId, x.CreatedAt });
    }
}
