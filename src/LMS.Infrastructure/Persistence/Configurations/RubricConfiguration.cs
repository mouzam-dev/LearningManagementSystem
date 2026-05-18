using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class RubricConfiguration : IEntityTypeConfiguration<Rubric>
{
    public void Configure(EntityTypeBuilder<Rubric> e)
    {
        e.ToTable("Rubrics");
        e.HasKey(x => x.Id);

        e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        e.Property(x => x.Description).HasMaxLength(2000);

        e.HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.Criteria)
            .WithOne(c => c.Rubric)
            .HasForeignKey(c => c.RubricId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasIndex(x => new { x.TeacherId, x.UpdatedAt });
    }
}

public class RubricCriterionConfiguration : IEntityTypeConfiguration<RubricCriterion>
{
    public void Configure(EntityTypeBuilder<RubricCriterion> e)
    {
        e.ToTable("RubricCriteria");
        e.HasKey(x => x.Id);

        e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        e.Property(x => x.Description).HasMaxLength(2000);

        e.HasIndex(x => new { x.RubricId, x.Order });
    }
}
