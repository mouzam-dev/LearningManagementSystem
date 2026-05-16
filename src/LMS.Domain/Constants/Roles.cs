namespace LMS.Domain.Constants;

/// <summary>
/// Canonical role names used in JWT claims, [Authorize(Roles=...)] attributes,
/// and the Users.Role column. Treat these strings as the single source of truth
/// so a typo in a controller can't silently grant or deny access.
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string OrgAdmin = "OrgAdmin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static readonly IReadOnlyCollection<string> All =
        new[] { SuperAdmin, OrgAdmin, Teacher, Student };
}
