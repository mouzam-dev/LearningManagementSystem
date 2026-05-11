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
}
