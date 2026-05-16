using FluentValidation;
using LMS.Application.Admin;
using LMS.Application.Common;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Application.SuperAdmin.Queries;
using LMS.Domain.Constants;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.SuperAdmin.Commands;

/// <summary>
/// Replace the permission set for a single role with the supplied list. Acts
/// as a full set-operation: codes present become granted, codes absent are
/// revoked. Existing user JWTs keep their old claims until they sign in again.
/// </summary>
public record SetRolePermissionsCommand(string Role, IReadOnlyList<string> PermissionCodes)
    : IRequest<RolePermissionMatrixDto>;

public class SetRolePermissionsCommandValidator : AbstractValidator<SetRolePermissionsCommand>
{
    public SetRolePermissionsCommandValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => Roles.All.Contains(r, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role must be one of: " + string.Join(", ", Roles.All));
        RuleFor(x => x.PermissionCodes).NotNull();
    }
}

public class SetRolePermissionsCommandHandler
    : IRequestHandler<SetRolePermissionsCommand, RolePermissionMatrixDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public SetRolePermissionsCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<RolePermissionMatrixDto> Handle(
        SetRolePermissionsCommand request, CancellationToken ct)
    {
        var canonicalRole = Roles.All.First(r =>
            string.Equals(r, request.Role, StringComparison.OrdinalIgnoreCase));

        var requested = request.PermissionCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allPerms = await _db.Permissions
            .ToDictionaryAsync(p => p.Code, p => p, StringComparer.OrdinalIgnoreCase, ct);

        var unknown = requested.Where(c => !allPerms.ContainsKey(c)).ToList();
        if (unknown.Count > 0)
        {
            throw new InvalidOperationException(
                "Unknown permission code(s): " + string.Join(", ", unknown));
        }

        // Diff existing rows against the requested set and apply the delta.
        var existing = await _db.RolePermissions
            .Where(rp => rp.Role == canonicalRole)
            .ToListAsync(ct);
        var existingCodeToRow = existing.ToDictionary(
            rp => allPerms.First(kv => kv.Value.Id == rp.PermissionId).Key,
            StringComparer.OrdinalIgnoreCase);

        var toAdd = requested.Where(c => !existingCodeToRow.ContainsKey(c)).ToList();
        var toRemove = existingCodeToRow
            .Where(kv => !requested.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
            .Select(kv => kv.Value)
            .ToList();

        foreach (var code in toAdd)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                Role = canonicalRole,
                PermissionId = allPerms[code].Id,
            });
        }
        foreach (var row in toRemove)
        {
            _db.RolePermissions.Remove(row);
        }

        if (toAdd.Count > 0 || toRemove.Count > 0)
        {
            _db.AuditLogs.Add(AdminAudit.Entry(
                _currentUser.GetUserId(),
                "role_permissions.updated",
                "RolePermissions",
                Guid.Empty,
                new
                {
                    role = canonicalRole,
                    granted = toAdd,
                    revoked = toRemove
                        .Select(r => allPerms.First(kv => kv.Value.Id == r.PermissionId).Key)
                        .ToArray(),
                }));

            await _db.SaveChangesAsync(ct);
        }

        return await _mediator.Send(new GetRolePermissionMatrixQuery(), ct);
    }
}
