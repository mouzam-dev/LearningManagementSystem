using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> e)
    {
        e.ToTable("AttendanceRecords");

        e.HasKey(x => x.Id);

        e.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        e.Property(x => x.Remark).HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        // Exactly one record per (session, student).
        e.HasIndex(x => new { x.AttendanceSessionId, x.StudentId }).IsUnique();

        // Reporting hot paths. CourseId/BranchId/SessionDate are denormalized plain
        // columns (no FK/navigation) so a Course delete doesn't create a second
        // cascade path to this table beyond the one through Session.
        e.HasIndex(x => new { x.StudentId, x.SessionDate });
        e.HasIndex(x => new { x.CourseId, x.SessionDate });
        e.HasIndex(x => new { x.BranchId, x.SessionDate });

        e.HasOne(x => x.Session)
            .WithMany(s => s.Records)
            .HasForeignKey(x => x.AttendanceSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict breaks the multiple-cascade-path cycle to User (same reason as
        // CourseRatings / Enrollments).
        e.HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
