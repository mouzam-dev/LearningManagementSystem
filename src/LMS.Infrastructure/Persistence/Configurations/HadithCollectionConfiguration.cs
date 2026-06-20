using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class HadithCollectionConfiguration : IEntityTypeConfiguration<HadithCollection>
{
    public void Configure(EntityTypeBuilder<HadithCollection> e)
    {
        e.ToTable("HadithCollections");

        e.HasKey(x => x.Slug);
        e.Property(x => x.Slug).HasMaxLength(50);
        e.Property(x => x.TitleEn).HasMaxLength(200).IsRequired();
        e.Property(x => x.TitleAr).HasMaxLength(200);
        e.Property(x => x.Source).HasMaxLength(20);
    }
}
