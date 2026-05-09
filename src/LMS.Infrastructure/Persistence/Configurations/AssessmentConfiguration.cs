using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> e)
    {
        e.ToTable("Assessments");
        e.HasKey(x => x.Id);

        e.Property(x => x.Title).IsRequired().HasMaxLength(200);
        e.Property(x => x.Type).IsRequired().HasMaxLength(20);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        e.HasOne(x => x.Course)
            .WithMany(c => c.Assessments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => x.CourseId);
        e.HasIndex(x => x.DueDate);
    }
}
