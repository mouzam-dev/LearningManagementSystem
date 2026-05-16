namespace LMS.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // see LMS.Domain.Constants.Roles
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;

    // Tenancy. SuperAdmin has both null (platform-wide). OrgAdmin has only
    // OrganizationId set (org-wide, no specific branch). Teacher/Student have both.
    public Guid? OrganizationId { get; set; }
    public Guid? BranchId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Organization? Organization { get; set; }
    public Branch? Branch { get; set; }

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    public ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
