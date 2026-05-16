using FluentValidation;
using LMS.Application.Admin.Dtos;
using LMS.Application.Admin.Queries;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Commands;

internal static class AdminUserRules
{
    public static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Student", "Teacher", "OrgAdmin", "SuperAdmin",
    };

    public static bool IsValidRole(string? r) =>
        !string.IsNullOrWhiteSpace(r) && AllowedRoles.Contains(r);
}

// ---------------- Suspend / reactivate -------------------------------------

public record SetUserActiveCommand(Guid UserId, bool IsActive)
    : IRequest<AdminUserDetailDto>;

public class SetUserActiveCommandHandler
    : IRequestHandler<SetUserActiveCommand, AdminUserDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public SetUserActiveCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<AdminUserDetailDto> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} was not found.");

        // Don't let an admin suspend themselves and lock the system out.
        if (request.IsActive == false && user.Id == _currentUser.GetUserId())
        {
            throw new InvalidOperationException("You can't suspend your own account.");
        }

        var was = user.IsActive;
        if (was != request.IsActive)
        {
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            _db.AuditLogs.Add(AdminAudit.Entry(
                _currentUser.GetUserId(),
                request.IsActive ? "user.reactivated" : "user.suspended",
                "User",
                user.Id,
                new { user.Email, name = (user.FirstName + " " + user.LastName).Trim() }));

            await _db.SaveChangesAsync(cancellationToken);
        }

        return await _mediator.Send(new GetAdminUserDetailQuery(user.Id), cancellationToken);
    }
}

// ---------------- Change role ----------------------------------------------

/// <summary>
/// Change a user's role, optionally re-homing them under a different organization
/// or branch. The tenancy fields are required for the roles that need them:
///   - SuperAdmin: ignores OrganizationId/BranchId (always cleared).
///   - OrgAdmin: requires OrganizationId; BranchId is cleared.
///   - Teacher/Student: requires BranchId (OrganizationId is derived from it).
/// </summary>
public record ChangeUserRoleCommand(
    Guid UserId,
    string Role,
    Guid? OrganizationId = null,
    Guid? BranchId = null) : IRequest<AdminUserDetailDto>;

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.Role).Must(AdminUserRules.IsValidRole)
            .WithMessage("Role must be one of: Student, Teacher, OrgAdmin, SuperAdmin.");

        RuleFor(x => x.OrganizationId)
            .NotEmpty()
            .When(x => string.Equals(x.Role, "OrgAdmin", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Pick an organization for the org admin.");

        RuleFor(x => x.BranchId)
            .NotEmpty()
            .When(x => string.Equals(x.Role, "Teacher", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.Role, "Student", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Pick a branch for the teacher or student.");
    }
}

public class ChangeUserRoleCommandHandler
    : IRequestHandler<ChangeUserRoleCommand, AdminUserDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public ChangeUserRoleCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<AdminUserDetailDto> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} was not found.");

        // Don't let an admin demote themselves and lock everyone else out.
        var newRole = request.Role.Trim();
        if (user.Id == _currentUser.GetUserId() && !string.Equals(user.Role, newRole, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("You can't change your own role.");
        }

        // Avoid demoting the last remaining SuperAdmin.
        if (string.Equals(user.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(newRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            var otherSuperAdmins = await _db.Users
                .CountAsync(u => u.Role == "SuperAdmin" && u.Id != user.Id && u.IsActive, cancellationToken);
            if (otherSuperAdmins == 0)
            {
                throw new InvalidOperationException("Can't demote the last active super admin.");
            }
        }

        var previousRole = user.Role;
        var previousOrgId = user.OrganizationId;
        var previousBranchId = user.BranchId;

        // Resolve the new tenancy from the requested role. SuperAdmin is platform-wide
        // (no org/branch). OrgAdmin attaches to an organization but no specific branch.
        // Teacher/Student attach to a branch; the org is derived to keep them consistent.
        Guid? newOrgId = previousOrgId;
        Guid? newBranchId = previousBranchId;

        if (string.Equals(newRole, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            newOrgId = null;
            newBranchId = null;
        }
        else if (string.Equals(newRole, "OrgAdmin", StringComparison.OrdinalIgnoreCase))
        {
            var targetOrgId = request.OrganizationId!.Value;
            var orgExists = await _db.Organizations
                .AnyAsync(o => o.Id == targetOrgId, cancellationToken);
            if (!orgExists)
            {
                throw new KeyNotFoundException($"Organization {targetOrgId} was not found.");
            }
            newOrgId = targetOrgId;
            newBranchId = null;
        }
        else // Teacher or Student
        {
            var targetBranchId = request.BranchId!.Value;
            var branch = await _db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == targetBranchId, cancellationToken)
                ?? throw new KeyNotFoundException($"Branch {targetBranchId} was not found.");
            newBranchId = branch.Id;
            newOrgId = branch.OrganizationId;
        }

        var roleChanged = !string.Equals(previousRole, newRole, StringComparison.Ordinal);
        var tenancyChanged = previousOrgId != newOrgId || previousBranchId != newBranchId;

        if (roleChanged || tenancyChanged)
        {
            user.Role = newRole;
            user.OrganizationId = newOrgId;
            user.BranchId = newBranchId;
            user.UpdatedAt = DateTime.UtcNow;

            _db.AuditLogs.Add(AdminAudit.Entry(
                _currentUser.GetUserId(),
                "user.role_changed",
                "User",
                user.Id,
                new
                {
                    user.Email,
                    from = previousRole,
                    to = newRole,
                    organizationId = newOrgId,
                    branchId = newBranchId,
                }));

            await _db.SaveChangesAsync(cancellationToken);
        }

        return await _mediator.Send(new GetAdminUserDetailQuery(user.Id), cancellationToken);
    }
}
