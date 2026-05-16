using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> e)
    {
        e.ToTable("Branches");
        e.HasKey(x => x.Id);

        e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        e.Property(x => x.Code).HasMaxLength(40);
        e.Property(x => x.Location).HasMaxLength(200);
        e.Property(x => x.ContactEmail).HasMaxLength(256);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        e.HasIndex(x => x.OrganizationId);
        e.HasIndex(x => new { x.OrganizationId, x.Code })
            .IsUnique()
            .HasFilter("[Code] IS NOT NULL");
    }
}
