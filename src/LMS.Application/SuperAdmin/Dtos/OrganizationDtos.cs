namespace LMS.Application.SuperAdmin.Dtos;

public class OrganizationListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; }
    public int BranchCount { get; set; }
    public int UserCount { get; set; }
    public int OrgAdminCount { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrganizationDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ContactEmail { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int BranchCount { get; set; }
    public int OrgAdminCount { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public List<BranchListItemDto> Branches { get; set; } = new();
    public List<OrgAdminListItemDto> OrgAdmins { get; set; } = new();
}

public class BranchListItemDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Location { get; set; }
    public string? ContactEmail { get; set; }
    public bool IsActive { get; set; }
    public int TeacherCount { get; set; }
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrgAdminListItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
