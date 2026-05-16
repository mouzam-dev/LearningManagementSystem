namespace LMS.Application.SuperAdmin.Dtos;

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RolePermissionMatrixDto
{
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<PermissionDto> Permissions { get; set; } = Array.Empty<PermissionDto>();
    /// <summary>Map of role -> set of permission codes granted to that role.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Grants { get; set; }
        = new Dictionary<string, IReadOnlyList<string>>();
}

public class SetRolePermissionsRequest
{
    public string Role { get; set; } = string.Empty;
    public IReadOnlyList<string> PermissionCodes { get; set; } = Array.Empty<string>();
}
