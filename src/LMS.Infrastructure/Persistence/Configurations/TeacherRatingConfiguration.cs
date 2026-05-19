using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class TeacherRatingConfiguration : IEntityTypeConfiguration<TeacherRating>
{
    public void Configure(EntityTypeBuilder<TeacherRating> e)
    {
        e.ToTable("TeacherRatings", t => t.HasCheckConstraint(
            "CK_TeacherRatings_Rating_Range", "[Rating] BETWEEN 1 AND 5"));

        e.HasKey(x => x.Id);

        e.Property(x => x.Rating).IsRequired();
        e.Property(x => x.Review).IsRequired().HasMaxLength(2000);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // One rating per (teacher, student, course). A student can leave a
        // teacher rating for each course they take with that teacher — letting
        // the experience differ by class. Resubmitting updates the existing row.
        e.HasIndex(x => new { x.TeacherId, x.StudentId, x.CourseId }).IsUnique();
        e.HasIndex(x => x.TeacherId);

        e.HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
