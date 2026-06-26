using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> e)
    {
        e.ToTable("Certificates");
        e.HasKey(x => x.Id);

        e.Property(x => x.VerifyCode).IsRequired().HasMaxLength(50);
        e.Property(x => x.IssuedAt).HasDefaultValueSql("now() at time zone 'utc'");

        e.HasOne(x => x.User)
            .WithMany(u => u.Certificates)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasOne(x => x.Course)
            .WithMany(c => c.Certificates)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasIndex(x => x.VerifyCode).IsUnique();
        e.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
    }
}
