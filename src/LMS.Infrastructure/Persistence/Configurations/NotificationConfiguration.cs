using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> e)
    {
        e.ToTable("Notifications");
        e.HasKey(x => x.Id);

        e.Property(x => x.Type).IsRequired().HasMaxLength(50);
        e.Property(x => x.Title).IsRequired().HasMaxLength(200);
        e.Property(x => x.Message).IsRequired().HasColumnType("nvarchar(max)");
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        e.HasOne(x => x.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
    }
}
