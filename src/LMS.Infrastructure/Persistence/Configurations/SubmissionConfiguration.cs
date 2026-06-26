using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> e)
    {
        e.ToTable("Submissions");
        e.HasKey(x => x.Id);

        e.Property(x => x.Answers).IsRequired().HasColumnType("text");
        e.Property(x => x.Feedback).HasColumnType("text");
        e.Property(x => x.SubmittedAt).HasDefaultValueSql("now() at time zone 'utc'");

        e.HasOne(x => x.Student)
            .WithMany(u => u.Submissions)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasOne(x => x.Assessment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(x => x.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.StudentId, x.AssessmentId });
    }
}
