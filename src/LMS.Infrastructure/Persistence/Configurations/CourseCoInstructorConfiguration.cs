using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class CourseCoInstructorConfiguration : IEntityTypeConfiguration<CourseCoInstructor>
{
    public void Configure(EntityTypeBuilder<CourseCoInstructor> e)
    {
        e.ToTable("CourseCoInstructors");

        // Composite key — one row per (course, user) pair.
        e.HasKey(x => new { x.CourseId, x.UserId });

        e.Property(x => x.AddedAt).HasDefaultValueSql("now() at time zone 'utc'");

        e.HasOne(x => x.Course)
            .WithMany(c => c.CoInstructors)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // No back-reference on User (no User.CoInstructorOn collection) —
        // queries always go from a Course or filter by UserId directly.
        e.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict); // avoid cascade cycle via Users

        e.HasIndex(x => x.UserId);
    }
}
