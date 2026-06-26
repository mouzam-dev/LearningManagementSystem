using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> e)
    {
        e.ToTable("LiveSessions");

        e.HasKey(x => x.Id);

        e.Property(x => x.Title).IsRequired().HasMaxLength(200);
        e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        e.Property(x => x.Provider).IsRequired().HasMaxLength(32);
        e.Property(x => x.RoomName).IsRequired().HasMaxLength(120);
        e.Property(x => x.DurationMinutes).HasDefaultValue(60);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("now() at time zone 'utc'");

        e.HasIndex(x => new { x.CourseId, x.ScheduledStart });
        e.HasIndex(x => new { x.BranchId, x.ScheduledStart });
        e.HasIndex(x => x.RoomName).IsUnique();

        e.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Secondary FKs Restrict to avoid multiple cascade paths to User and to
        // keep Org/Branch deletes from silently wiping live-class history.
        e.HasOne(x => x.HostTeacher)
            .WithMany()
            .HasForeignKey(x => x.HostTeacherId)
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
