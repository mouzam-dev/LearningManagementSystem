using LMS.Application.Common;

namespace LMS.Application.OrgAdmin;

/// <summary>
/// Pulls the OrgAdmin's organization id off the current JWT and throws if it's
/// missing. Every OrgAdmin handler calls this on entry so a malformed token
/// can't accidentally widen the query to "all orgs".
/// </summary>
internal static class OrgAdminScope
{
    public static Guid RequireOrganizationId(ICurrentUserService currentUser)
    {
        var orgId = currentUser.GetOrganizationId();
        if (orgId is null || orgId.Value == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "Caller has no organization claim. Sign out and back in.");
        }
        return orgId.Value;
    }
}
