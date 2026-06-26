using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> e)
    {
        e.ToTable("UserTokens");
        e.HasKey(x => x.Id);

        e.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        e.Property(x => x.Purpose).IsRequired().HasMaxLength(40);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");

        // Lookup by hash (single-use) — fast path on every redeem.
        e.HasIndex(x => x.TokenHash).IsUnique();
        // Latest-token-per-user-per-purpose query (resend etc.).
        e.HasIndex(x => new { x.UserId, x.Purpose, x.CreatedAt });

        e.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
