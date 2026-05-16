using LMS.Domain.Constants;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent tenancy bootstrap that runs once per app startup after EF migrations.
///
/// Three jobs, in order:
///   1) Sync the Permissions + RolePermissions tables to match the code-side constants
///      in <c>LMS.Domain.Constants.Permissions</c>. Adding a new permission to a role
///      flows through without a manual SQL step.
///   2) Ensure a Default Organization + Default Branch exist (tenant of last resort
///      for users created before multi-tenancy or via the public register flow).
///   3) Repair legacy data — rename any leftover Role='Admin' rows to 'SuperAdmin'
///      and assign Teachers/Students without a Branch to the Default Branch.
/// </summary>
public static class TenancySeeder
{
    public static readonly Guid DefaultOrganizationId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        await SyncPermissionsAsync(db, ct);
        await SyncRolePermissionsAsync(db, ct);
        await EnsureDefaultTenantAsync(db, ct);
        var renamed = await RenameLegacyAdminAsync(db, ct);
        var assigned = await BackfillUserTenancyAsync(db, ct);

        await db.SaveChangesAsync(ct);

        if (renamed > 0)
        {
            logger.LogInformation("Renamed {Count} legacy Admin user(s) to SuperAdmin.", renamed);
        }
        if (assigned > 0)
        {
            logger.LogInformation("Assigned {Count} legacy user(s) to the Default Branch.", assigned);
        }
    }

    // -------------------------------------------------------------------------
    // Permissions table: insert any missing row, update description if changed,
    // never delete (orphaned permissions are harmless and may be referenced).
    // -------------------------------------------------------------------------
    private static async Task SyncPermissionsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Code, ct);
        foreach (var code in Permissions.All)
        {
            var description = DescribePermission(code);
            if (!existing.TryGetValue(code, out var row))
            {
                db.Permissions.Add(new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Description = description,
                });
            }
            else if (row.Description != description)
            {
                row.Description = description;
            }
        }
    }

    // -------------------------------------------------------------------------
    // RolePermissions: ensure each role has the rows defined in DefaultsByRole.
    // Extra rows (e.g. a SuperAdmin granted ad-hoc rights) are left alone.
    // -------------------------------------------------------------------------
    private static async Task SyncRolePermissionsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        // Need permission ids — call SaveChanges first if we added new permissions above.
        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
        }

        var permsByCode = await db.Permissions.ToDictionaryAsync(p => p.Code, ct);
        var existing = await db.RolePermissions
            .Select(rp => new { rp.Role, rp.PermissionId })
            .ToListAsync(ct);
        var existingSet = existing
            .Select(x => (x.Role.ToLowerInvariant(), x.PermissionId))
            .ToHashSet();

        foreach (var (role, codes) in Permissions.DefaultsByRole)
        {
            foreach (var code in codes)
            {
                if (!permsByCode.TryGetValue(code, out var perm)) continue;
                if (existingSet.Contains((role.ToLowerInvariant(), perm.Id))) continue;

                db.RolePermissions.Add(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    Role = role,
                    PermissionId = perm.Id,
                });
            }
        }
    }

    // -------------------------------------------------------------------------
    // Default Organization + Default Branch with deterministic ids so the seeder
    // is safe to run repeatedly across containers / fresh volumes.
    // -------------------------------------------------------------------------
    private static async Task EnsureDefaultTenantAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var orgExists = await db.Organizations.AnyAsync(o => o.Id == DefaultOrganizationId, ct);
        if (!orgExists)
        {
            db.Organizations.Add(new Organization
            {
                Id = DefaultOrganizationId,
                Name = "Default Organization",
                Slug = "default",
                Description = "Tenant of last resort. Users without an explicit organization land here.",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        var branchExists = await db.Branches.AnyAsync(b => b.Id == DefaultBranchId, ct);
        if (!branchExists)
        {
            db.Branches.Add(new Branch
            {
                Id = DefaultBranchId,
                OrganizationId = DefaultOrganizationId,
                Name = "Default Branch",
                Code = "DEFAULT",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
    }

    // -------------------------------------------------------------------------
    // Promote every legacy Admin row to SuperAdmin. Idempotent.
    // -------------------------------------------------------------------------
    private static async Task<int> RenameLegacyAdminAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var legacy = await db.Users.Where(u => u.Role == "Admin").ToListAsync(ct);
        foreach (var u in legacy)
        {
            u.Role = Roles.SuperAdmin;
            u.UpdatedAt = DateTime.UtcNow;
        }
        return legacy.Count;
    }

    // -------------------------------------------------------------------------
    // Any Teacher/Student/OrgAdmin that doesn't yet belong to a branch lands
    // in the Default Branch so cross-tenant queries don't have to deal with NULLs.
    // SuperAdmin is intentionally left null — they're platform-wide.
    // -------------------------------------------------------------------------
    private static async Task<int> BackfillUserTenancyAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var roles = new[] { Roles.OrgAdmin, Roles.Teacher, Roles.Student };
        var orphans = await db.Users
            .Where(u => roles.Contains(u.Role) && (u.BranchId == null || u.OrganizationId == null))
            .ToListAsync(ct);

        foreach (var u in orphans)
        {
            u.OrganizationId = DefaultOrganizationId;
            u.BranchId = DefaultBranchId;
            u.UpdatedAt = DateTime.UtcNow;
        }
        return orphans.Count;
    }

    private static string DescribePermission(string code) => code switch
    {
        Permissions.OrgRead => "View organizations.",
        Permissions.OrgCreate => "Create organizations.",
        Permissions.OrgUpdate => "Update organizations.",
        Permissions.OrgDelete => "Delete organizations.",
        Permissions.BranchRead => "View branches.",
        Permissions.BranchCreate => "Create branches.",
        Permissions.BranchUpdate => "Update branches.",
        Permissions.BranchDelete => "Delete branches.",
        Permissions.OrgAdminCreate => "Create organization administrators.",
        Permissions.OrgAdminList => "List organization administrators.",
        Permissions.TeacherCreate => "Create teachers within an organization.",
        Permissions.TeacherList => "List teachers within an organization.",
        Permissions.CourseModerate => "Moderate (publish/unpublish/delete) courses in an organization.",
        _ => code,
    };
}
