using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("Users");
        e.HasKey(x => x.Id);

        e.Property(x => x.Email).IsRequired().HasMaxLength(256);
        e.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
        e.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        e.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        e.Property(x => x.Role).IsRequired().HasMaxLength(20);
        e.Property(x => x.ProfilePictureUrl).HasMaxLength(500);
        e.Property(x => x.Bio).HasMaxLength(1000);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        e.HasIndex(x => x.Email).IsUnique();
        e.HasIndex(x => x.Role);
    }
}
