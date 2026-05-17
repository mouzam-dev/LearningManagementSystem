using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> e)
    {
        e.ToTable("Courses");
        e.HasKey(x => x.Id);

        e.Property(x => x.Title).IsRequired().HasMaxLength(200);
        e.Property(x => x.Description).IsRequired();
        e.Property(x => x.Category).IsRequired().HasMaxLength(100);
        e.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        e.HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.NoAction); // avoid cascade cycles via User

        e.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => x.Title);
        e.HasIndex(x => x.Category);
        e.HasIndex(x => x.TeacherId);
        e.HasIndex(x => x.IsPublished);
        e.HasIndex(x => x.IsArchived);
        e.HasIndex(x => x.OrganizationId);
        e.HasIndex(x => x.BranchId);
    }
}
