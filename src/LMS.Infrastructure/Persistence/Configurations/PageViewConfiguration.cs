using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class PageViewConfiguration : IEntityTypeConfiguration<PageView>
{
    public void Configure(EntityTypeBuilder<PageView> e)
    {
        e.ToTable("PageViews");
        e.HasKey(x => x.Id);
        e.Property(x => x.Path).HasMaxLength(200);
        e.Property(x => x.Role).HasMaxLength(20);
        e.Property(x => x.Device).HasMaxLength(10);
        e.HasIndex(x => x.CreatedAt);              // every report filters by date range
        e.HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}
