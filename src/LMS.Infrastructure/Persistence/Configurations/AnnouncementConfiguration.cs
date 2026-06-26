using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> e)
    {
        e.ToTable("Announcements");
        e.HasKey(x => x.Id);

        e.Property(x => x.Title).IsRequired().HasMaxLength(200);
        e.Property(x => x.Body).IsRequired().HasColumnType("text");
        e.Property(x => x.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("now() at time zone 'utc'");

        e.HasIndex(x => new { x.CourseId, x.CreatedAt });

        e.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
