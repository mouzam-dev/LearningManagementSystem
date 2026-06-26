using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class CourseRatingConfiguration : IEntityTypeConfiguration<CourseRating>
{
    public void Configure(EntityTypeBuilder<CourseRating> e)
    {
        e.ToTable("CourseRatings", t => t.HasCheckConstraint(
            "CK_CourseRatings_Rating_Range", "\"Rating\" BETWEEN 1 AND 5"));

        e.HasKey(x => x.Id);

        e.Property(x => x.Rating).IsRequired();
        e.Property(x => x.Review).IsRequired().HasMaxLength(2000);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("now() at time zone 'utc'");

        // One rating per (course, student). Upsert in the handler enforces this
        // at the app level; the unique index is the DB-side safety net.
        e.HasIndex(x => new { x.CourseId, x.StudentId }).IsUnique();
        e.HasIndex(x => x.CourseId);

        e.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // SQL Server forbids multiple cascade paths to User (already cascading
        // via Enrollments etc.), so restrict here to break the cycle.
        e.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
