using Microsoft.AspNetCore.Authorization;

namespace LMS.WebAPI.Authorization;

/// <summary>
/// Restrict an endpoint to callers whose JWT carries a <c>perm</c> claim with
/// the given code. Composes with <c>[Authorize]</c>: the user must still be
/// authenticated; this attribute only adds the permission check.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = PolicyPrefix + permission;
    }

    public string Permission { get; }
}
