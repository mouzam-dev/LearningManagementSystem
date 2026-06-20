using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> e)
    {
        e.ToTable("AttendanceSessions", t => t.HasCheckConstraint(
            "CK_AttendanceSessions_Slot_Positive", "[Slot] >= 1"));

        e.HasKey(x => x.Id);

        // Enum stored as its name for readable rows + reorder-safety.
        e.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        e.Property(x => x.Topic).HasMaxLength(200);
        e.Property(x => x.Slot).HasDefaultValue(1);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // One session per (course, date, slot). Slot supports multiple batches/day.
        e.HasIndex(x => new { x.CourseId, x.SessionDate, x.Slot }).IsUnique();
        // Branch- and org-wide reporting by date.
        e.HasIndex(x => new { x.BranchId, x.SessionDate });
        e.HasIndex(x => new { x.OrganizationId, x.SessionDate });
        // Fast lookup of the attendance session auto-created for a live class.
        e.HasIndex(x => x.LiveSessionId);

        e.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Secondary FKs are Restrict to avoid multiple cascade paths to User and to
        // keep Org/Branch deletes from silently wiping attendance history.
        e.HasOne(x => x.TakenByTeacher)
            .WithMany()
            .HasForeignKey(x => x.TakenByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
