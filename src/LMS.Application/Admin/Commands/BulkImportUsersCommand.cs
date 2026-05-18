using System.Security.Cryptography;
using FluentValidation;
using LMS.Application.Admin.Dtos;
using LMS.Application.Auth;
using LMS.Application.Common;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Commands;

/// <summary>
/// Bulk-create users from a parsed CSV (BRD ADM-012). Each row is validated
/// independently and either:
///   - succeeds: user is created with a random unguessable password, marked
///     verified + active, then issued a password-reset email so they pick
///     their own password on first sign-in.
///   - fails: error message captured, OTHER rows continue processing.
///
/// We never roll the whole import back for one bad row — admins generally
/// want partial progress over an all-or-nothing failure, and the per-row
/// result table tells them exactly what to fix and re-upload.
/// </summary>
public record BulkImportUsersCommand(IReadOnlyList<BulkImportUserRow> Rows)
    : IRequest<BulkImportUsersResultDto>;

public class BulkImportUsersCommandValidator : AbstractValidator<BulkImportUsersCommand>
{
    public BulkImportUsersCommandValidator()
    {
        RuleFor(x => x.Rows).NotNull()
            .Must(r => r.Count >= 1).WithMessage("CSV had no data rows after the header.")
            .Must(r => r.Count <= 1000).WithMessage("Import up to 1000 rows at a time.");
    }
}

public class BulkImportUsersCommandHandler
    : IRequestHandler<BulkImportUsersCommand, BulkImportUsersResultDto>
{
    // Roles we allow via CSV. SuperAdmin is deliberately excluded — promoting
    // someone to platform admin should be a deliberate, audited UI action.
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Student", "Teacher", "OrgAdmin",
    };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccountLifecycleService _accountLifecycle;

    public BulkImportUsersCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IPasswordHasher passwordHasher,
        IAccountLifecycleService accountLifecycle)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _accountLifecycle = accountLifecycle;
    }

    /// <summary>
    /// In-memory snapshot of an organization for the import. <see cref="DefaultBranchId"/>
    /// is the oldest branch in the org (by CreatedAt) — used when a Teacher/Student
    /// row omits BranchName.
    /// </summary>
    private record OrgInfo(Guid Id, string? Slug, string Name, Guid? DefaultBranchId);

    public async Task<BulkImportUsersResultDto> Handle(
        BulkImportUsersCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetUserId();
        var now = DateTime.UtcNow;

        // Preload the existing email set so per-row uniqueness checks are O(1)
        // instead of a query per row.
        var existingEmails = await _db.Users
            .AsNoTracking()
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);
        var emailSet = new HashSet<string>(existingEmails, StringComparer.OrdinalIgnoreCase);

        // Preload branches once for: (a) name lookup, (b) default-branch-per-org.
        var branches = await _db.Branches
            .AsNoTracking()
            .Select(b => new { b.Id, b.Name, b.OrganizationId, b.CreatedAt })
            .ToListAsync(cancellationToken);
        var branchByOrgAndName = branches
            .GroupBy(b => b.OrganizationId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(b => b.Name, b => b.Id, StringComparer.OrdinalIgnoreCase));
        var defaultBranchPerOrg = branches
            .GroupBy(b => b.OrganizationId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(b => b.CreatedAt).First().Id);

        // Preload orgs and attach each org's default branch (oldest one in the org).
        var rawOrgs = await _db.Organizations
            .AsNoTracking()
            .Select(o => new { o.Id, o.Slug, o.Name })
            .ToListAsync(cancellationToken);
        var orgBySlug = new Dictionary<string, OrgInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in rawOrgs)
        {
            if (string.IsNullOrWhiteSpace(o.Slug)) continue;
            defaultBranchPerOrg.TryGetValue(o.Id, out var defBranchId);
            orgBySlug[o.Slug!] = new OrgInfo(o.Id, o.Slug, o.Name, defBranchId == default ? null : defBranchId);
        }

        // Default org is the seeded fallback (Slug = "default"). When a row
        // omits OrganizationSlug, we fall back to this org. If it's missing,
        // the affected rows will fail with a clear message and the rest
        // continue.
        var defaultOrg = orgBySlug.GetValueOrDefault("default");

        var results = new List<BulkImportRowResultDto>(request.Rows.Count);
        var createdEmailsForReset = new List<string>();

        foreach (var row in request.Rows)
        {
            var rowResult = new BulkImportRowResultDto
            {
                Line = row.Line,
                Email = row.Email,
            };

            try
            {
                var (user, err) = TryBuildUser(row, emailSet, orgBySlug, branchByOrgAndName, defaultOrg);
                if (err is not null)
                {
                    rowResult.Status = "Failed";
                    rowResult.Error = err;
                    results.Add(rowResult);
                    continue;
                }

                user!.Id = Guid.NewGuid();
                user.PasswordHash = _passwordHasher.Hash(GenerateStrongPassword());
                user.IsVerified = true;
                user.IsActive = true;
                user.CreatedAt = now;
                user.UpdatedAt = now;

                _db.Users.Add(user);
                emailSet.Add(user.Email); // prevent dup-within-same-csv collisions

                _db.AuditLogs.Add(AdminAudit.Entry(
                    actorId,
                    "user.bulk_imported",
                    "User",
                    user.Id,
                    new { user.Email, role = user.Role, organizationId = user.OrganizationId, branchId = user.BranchId }));

                rowResult.Status = "Created";
                rowResult.UserId = user.Id;
                results.Add(rowResult);
                createdEmailsForReset.Add(user.Email);
            }
            catch (Exception ex)
            {
                rowResult.Status = "Failed";
                rowResult.Error = $"Unexpected error: {ex.Message}";
                results.Add(rowResult);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Fire password-reset emails AFTER the save so we don't email phantoms
        // if SaveChanges throws. IssuePasswordResetAsync swallows unknown-email
        // errors anyway (anti-enumeration), so we don't need to gate on success.
        foreach (var email in createdEmailsForReset)
        {
            try
            {
                await _accountLifecycle.IssuePasswordResetAsync(email, cancellationToken);
            }
            catch
            {
                // Email failures shouldn't fail the import — the user record
                // exists and the admin can re-trigger a reset from /admin/users.
            }
        }

        return new BulkImportUsersResultDto
        {
            TotalRows = request.Rows.Count,
            SuccessCount = results.Count(r => r.Status == "Created"),
            FailureCount = results.Count(r => r.Status == "Failed"),
            Rows = results,
        };
    }

    private static (User? user, string? error) TryBuildUser(
        BulkImportUserRow row,
        HashSet<string> emailSet,
        Dictionary<string, OrgInfo> orgBySlug,
        Dictionary<Guid, Dictionary<string, Guid>> branchByOrgAndName,
        OrgInfo? defaultOrg)
    {
        // Required fields
        var email = row.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email)) return (null, "Email is required.");
        if (!email.Contains('@') || email.Length > 200) return (null, "Email looks invalid.");
        if (emailSet.Contains(email)) return (null, $"Email '{email}' is already in use.");

        var first = (row.FirstName ?? string.Empty).Trim();
        var last = (row.LastName ?? string.Empty).Trim();
        if (first.Length == 0) return (null, "FirstName is required.");
        if (last.Length == 0) return (null, "LastName is required.");
        if (first.Length > 100 || last.Length > 100) return (null, "First/Last name too long (max 100).");

        var role = (row.Role ?? string.Empty).Trim();
        if (!AllowedRoles.Contains(role))
        {
            return (null, $"Role must be one of: Student, Teacher, OrgAdmin (got '{row.Role}').");
        }
        // Canonicalize casing to match what the rest of the system stores.
        role = AllowedRoles.First(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

        // Resolve tenancy.
        var slug = (row.OrganizationSlug ?? string.Empty).Trim();
        OrgInfo? org;
        if (slug.Length > 0)
        {
            if (!orgBySlug.TryGetValue(slug, out org))
            {
                return (null, $"Organization slug '{slug}' was not found.");
            }
        }
        else
        {
            org = defaultOrg;
            if (org is null) return (null, "No organization slug given and no Default organization exists.");
        }

        Guid? branchId = null;
        var orgId = org!.Id;
        var branchName = (row.BranchName ?? string.Empty).Trim();
        if (role.Equals("Teacher", StringComparison.OrdinalIgnoreCase)
            || role.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            if (branchName.Length > 0)
            {
                if (!branchByOrgAndName.TryGetValue(orgId, out var inner)
                    || !inner.TryGetValue(branchName, out var bid))
                {
                    return (null, $"Branch '{branchName}' was not found in organization '{(slug.Length > 0 ? slug : "default")}'.");
                }
                branchId = bid;
            }
            else
            {
                branchId = org.DefaultBranchId;
                if (branchId is null)
                {
                    return (null, $"Organization '{(slug.Length > 0 ? slug : "default")}' has no branches; provide BranchName explicitly or create a branch first.");
                }
            }
        }
        // OrgAdmin: no branch (intentional).

        return (new User
        {
            Email = email,
            FirstName = first,
            LastName = last,
            Role = role,
            OrganizationId = orgId,
            BranchId = branchId,
        }, null);
    }

    /// <summary>
    /// 32-byte URL-safe random string. Stored hashed; user resets via the
    /// password-reset email they receive after the import completes.
    /// </summary>
    private static string GenerateStrongPassword()
    {
        Span<byte> buf = stackalloc byte[24];
        RandomNumberGenerator.Fill(buf);
        return Convert.ToBase64String(buf)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
