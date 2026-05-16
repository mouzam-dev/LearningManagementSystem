namespace LMS.Domain.Entities;

/// <summary>
/// Mapping of role-name -> permission. Role lives as a string (matching User.Role)
/// rather than a separate Roles table — the project's role set is small, well-known,
/// and changes only with a code deploy. The permission codes are the variable part.
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
