namespace LMS.Application.OrgAdmin.Dtos;

public class OrgAdminDashboardDto
{
    public Guid OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string? OrganizationSlug { get; set; }
    public string? OrganizationDescription { get; set; }
    public int BranchCount { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public int CourseCount { get; set; }
    public int PublishedCourseCount { get; set; }
    public IReadOnlyList<OrgAdminBranchSummary> Branches { get; set; } = Array.Empty<OrgAdminBranchSummary>();
}

public class OrgAdminBranchSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public bool IsActive { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
}

public class OrgTeacherDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int CourseCount { get; set; }
    public int PublishedCourseCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
