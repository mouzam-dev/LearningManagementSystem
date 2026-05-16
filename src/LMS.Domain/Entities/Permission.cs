namespace LMS.Domain.Entities;

/// <summary>
/// A single capability the API can check (e.g. "branch.create"). Codes are
/// constants in <c>LMS.Domain.Constants.Permissions</c>; this table holds them
/// so they can be referenced by RolePermission rows and managed by SuperAdmin
/// without redeploying.
/// </summary>
public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
