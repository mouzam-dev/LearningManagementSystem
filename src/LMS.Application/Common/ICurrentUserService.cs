namespace LMS.Application.Common;

/// <summary>
/// Surfaces the authenticated caller's identity to Application handlers without
/// dragging in an HttpContext dependency.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>True when an authenticated principal is on the request.</summary>
    bool IsAuthenticated { get; }

    /// <summary>JWT subject claim parsed as a Guid. Throws if missing/invalid.</summary>
    Guid GetUserId();

    /// <summary>Role claim from the JWT, or null when unauthenticated.</summary>
    string? GetRole();

    /// <summary>OrganizationId claim, or null for SuperAdmin / unauthenticated.</summary>
    Guid? GetOrganizationId();

    /// <summary>BranchId claim, or null for SuperAdmin / OrgAdmin / unauthenticated.</summary>
    Guid? GetBranchId();

    /// <summary>Whether the caller has been granted the given permission code.</summary>
    bool HasPermission(string permissionCode);
}
