using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> e)
    {
        e.ToTable("DeviceTokens");
        e.HasKey(x => x.Id);
        e.Property(x => x.Token).HasMaxLength(512).IsRequired();
        e.Property(x => x.Platform).HasMaxLength(20);
        e.HasIndex(x => x.Token).IsUnique();   // one phone = one row, re-pointed if another user signs in
        e.HasIndex(x => x.UserId);
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
