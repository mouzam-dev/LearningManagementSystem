using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LMS.WebAPI.Authorization;

/// <summary>
/// Materializes a permission policy on the fly the first time the framework
/// asks for <c>perm:&lt;code&gt;</c>. Avoids having to register every permission
/// up-front in <c>AddAuthorization</c>.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
        return _fallback.GetPolicyAsync(policyName);
    }
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) { Permission = permission; }
    public string Permission { get; }
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var has = context.User.Claims.Any(c =>
            c.Type == LmsClaims.Permission && c.Value == requirement.Permission);

        if (has)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
