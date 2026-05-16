using LMS.Application.Common;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.SuperAdmin.Queries;

public record GetRolePermissionMatrixQuery() : IRequest<RolePermissionMatrixDto>;

public class GetRolePermissionMatrixQueryHandler
    : IRequestHandler<GetRolePermissionMatrixQuery, RolePermissionMatrixDto>
{
    private readonly IApplicationDbContext _db;

    public GetRolePermissionMatrixQueryHandler(IApplicationDbContext db) { _db = db; }

    public async Task<RolePermissionMatrixDto> Handle(
        GetRolePermissionMatrixQuery request, CancellationToken ct)
    {
        var permissions = await _db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new PermissionDto { Id = p.Id, Code = p.Code, Description = p.Description })
            .ToListAsync(ct);

        var grants = await _db.RolePermissions
            .AsNoTracking()
            .Select(rp => new { rp.Role, Code = rp.Permission.Code })
            .ToListAsync(ct);

        var byRole = grants
            .GroupBy(g => g.Role, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(x => x.Code).OrderBy(c => c).ToList(),
                StringComparer.OrdinalIgnoreCase);

        // Ensure every known role appears in the response — even if it has zero
        // permissions today — so the UI can render an empty row instead of dropping it.
        foreach (var role in Roles.All)
        {
            if (!byRole.ContainsKey(role))
            {
                byRole[role] = Array.Empty<string>();
            }
        }

        return new RolePermissionMatrixDto
        {
            Roles = Roles.All.ToList(),
            Permissions = permissions,
            Grants = byRole,
        };
    }
}
