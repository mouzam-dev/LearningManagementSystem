using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> e)
    {
        e.ToTable("AuditLogs");
        e.HasKey(x => x.Id);

        e.Property(x => x.Action).IsRequired().HasMaxLength(100);
        e.Property(x => x.Entity).IsRequired().HasMaxLength(100);
        e.Property(x => x.Changes).HasColumnType("nvarchar(max)");
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        e.HasIndex(x => x.UserId);
        e.HasIndex(x => x.Entity);
        e.HasIndex(x => x.CreatedAt);
    }
}
