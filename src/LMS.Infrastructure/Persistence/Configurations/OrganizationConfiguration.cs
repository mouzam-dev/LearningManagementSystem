using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> e)
    {
        e.ToTable("Organizations");
        e.HasKey(x => x.Id);

        e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        e.Property(x => x.Slug).HasMaxLength(80);
        e.Property(x => x.Description).HasMaxLength(2000);
        e.Property(x => x.ContactEmail).HasMaxLength(256);
        e.Property(x => x.LogoUrl).HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        e.HasIndex(x => x.Slug).IsUnique().HasFilter("[Slug] IS NOT NULL");
        e.HasIndex(x => x.Name);

        e.HasMany(x => x.Branches)
            .WithOne(b => b.Organization)
            .HasForeignKey(b => b.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
