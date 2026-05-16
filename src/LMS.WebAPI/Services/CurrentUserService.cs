using System.Security.Claims;
using LMS.Application.Common;
using LMS.WebAPI.Authorization;

namespace LMS.WebAPI.Services;

/// <summary>
/// Reads the authenticated caller's identity from the current HttpContext. Lives
/// in the WebAPI project because that's where the HTTP plumbing is wired up.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid GetUserId()
    {
        var raw = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(raw, out var id))
        {
            throw new UnauthorizedAccessException("Authenticated user id claim is missing or not a Guid.");
        }
        return id;
    }

    public string? GetRole() => _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

    public Guid? GetOrganizationId() =>
        TryGuid(_accessor.HttpContext?.User?.FindFirstValue(LmsClaims.OrganizationId));

    public Guid? GetBranchId() =>
        TryGuid(_accessor.HttpContext?.User?.FindFirstValue(LmsClaims.BranchId));

    public bool HasPermission(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode)) return false;
        var user = _accessor.HttpContext?.User;
        if (user is null) return false;
        return user.Claims.Any(c => c.Type == LmsClaims.Permission && c.Value == permissionCode);
    }

    private static Guid? TryGuid(string? raw) =>
        Guid.TryParse(raw, out var g) ? g : null;
}
