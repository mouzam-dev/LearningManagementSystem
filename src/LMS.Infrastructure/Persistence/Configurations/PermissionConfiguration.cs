using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> e)
    {
        e.ToTable("Permissions");
        e.HasKey(x => x.Id);

        e.Property(x => x.Code).IsRequired().HasMaxLength(80);
        e.Property(x => x.Description).IsRequired().HasMaxLength(300);

        e.HasIndex(x => x.Code).IsUnique();
    }
}
