using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> e)
    {
        e.ToTable("Questions");
        e.HasKey(x => x.Id);

        e.Property(x => x.QuestionText).IsRequired().HasColumnType("text");
        e.Property(x => x.Type).IsRequired().HasMaxLength(20);
        e.Property(x => x.Options).HasColumnType("text");
        e.Property(x => x.CorrectAnswer).HasColumnType("text");

        e.HasOne(x => x.Assessment)
            .WithMany(a => a.Questions)
            .HasForeignKey(x => x.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.AssessmentId, x.Order });
    }
}
