using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> e)
    {
        e.ToTable("Enrollments");
        e.HasKey(x => x.Id);

        e.Property(x => x.ProgressPercentage).HasColumnType("decimal(5,2)").HasDefaultValue(0);
        e.Property(x => x.EnrolledAt).HasDefaultValueSql("now() at time zone 'utc'");

        e.HasOne(x => x.Student)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.NoAction); // cascade comes from Course side

        e.HasOne(x => x.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
    }
}
