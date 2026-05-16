namespace LMS.Domain.Constants;

/// <summary>
/// All permission codes the API knows about. Permissions live in the database
/// (Permissions table) and are mapped to roles via RolePermissions, but we
/// keep the canonical string list here so the seed and the [RequirePermission]
/// attributes can't drift apart.
/// </summary>
public static class Permissions
{
    // ---- Organization (SuperAdmin) ---------------------------------------
    public const string OrgRead = "org.read";
    public const string OrgCreate = "org.create";
    public const string OrgUpdate = "org.update";
    public const string OrgDelete = "org.delete";

    // ---- Branch (SuperAdmin + OrgAdmin within their org) -----------------
    public const string BranchRead = "branch.read";
    public const string BranchCreate = "branch.create";
    public const string BranchUpdate = "branch.update";
    public const string BranchDelete = "branch.delete";

    // ---- Org admin lifecycle (SuperAdmin) --------------------------------
    public const string OrgAdminCreate = "orgadmin.create";
    public const string OrgAdminList = "orgadmin.list";

    // ---- Teacher within an organization (OrgAdmin) -----------------------
    public const string TeacherCreate = "teacher.create";
    public const string TeacherList = "teacher.list";

    // ---- Course moderation within an org (OrgAdmin) ----------------------
    public const string CourseModerate = "course.moderate";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        OrgRead, OrgCreate, OrgUpdate, OrgDelete,
        BranchRead, BranchCreate, BranchUpdate, BranchDelete,
        OrgAdminCreate, OrgAdminList,
        TeacherCreate, TeacherList,
        CourseModerate,
    };

    /// <summary>
    /// Default role -> permission mapping seeded into RolePermissions.
    /// Reseeded on every app start (additive only) so adding a new permission
    /// to a role flows through without a manual migration.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> DefaultsByRole =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [Roles.SuperAdmin] = All.ToArray(),
            [Roles.OrgAdmin] = new[]
            {
                OrgRead,
                BranchRead, BranchCreate, BranchUpdate, BranchDelete,
                TeacherCreate, TeacherList,
                CourseModerate,
            },
            [Roles.Teacher] = Array.Empty<string>(),
            [Roles.Student] = Array.Empty<string>(),
        };
}
