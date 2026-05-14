namespace LMS.Application.Admin.Dtos;

public class AdminDashboardDto
{
    public AdminDashboardSummary Summary { get; set; } = new();
    public IReadOnlyList<RecentRegistrationDto> RecentRegistrations { get; set; } = Array.Empty<RecentRegistrationDto>();
    public IReadOnlyList<TopCourseDto> TopCourses { get; set; } = Array.Empty<TopCourseDto>();
}

public class AdminDashboardSummary
{
    // User counts
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int Students { get; set; }
    public int Teachers { get; set; }
    public int Admins { get; set; }
    public int RegistrationsLast7Days { get; set; }

    // Course / learning activity
    public int TotalCourses { get; set; }
    public int PublishedCourses { get; set; }
    public int DraftCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }
    public int CertificatesIssued { get; set; }
    public int TotalSubmissions { get; set; }
    public int UngradedSubmissions { get; set; }
}

public class RecentRegistrationDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TopCourseDto
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public bool IsPublished { get; set; }
}

// ---------------- Users ----------------------------------------------------

public class AdminUserListItemDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CoursesTaught { get; set; }      // teachers only — 0 otherwise
    public int Enrollments { get; set; }        // students only — 0 otherwise
    public int Certificates { get; set; }
}

public class AdminUserDetailDto
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Teacher-only summary (zeroed for non-teachers)
    public int CoursesTaught { get; set; }
    public int PublishedCourses { get; set; }
    public int TotalStudentsAcrossCourses { get; set; }

    // Student-only summary (zeroed for non-students)
    public int Enrollments { get; set; }
    public int CompletedCourses { get; set; }
    public int CertificatesEarned { get; set; }
    public int Submissions { get; set; }
}

/// <summary>Paginated wrapper for the user list — mirrors the existing PagedResult on the student side.</summary>
public class AdminUsersPage
{
    public IReadOnlyList<AdminUserListItemDto> Items { get; set; } = Array.Empty<AdminUserListItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
