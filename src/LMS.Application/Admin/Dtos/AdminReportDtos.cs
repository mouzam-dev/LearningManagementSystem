namespace LMS.Application.Admin.Dtos;

/// <summary>Platform-wide reporting payload for the admin reports page.</summary>
public class AdminReportDto
{
    public DateTime GeneratedAt { get; set; }

    // Headline totals
    public int TotalUsers { get; set; }
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public int TotalCertificates { get; set; }

    /// <summary>New registrations per month (last 6 months, oldest → newest), split by role.</summary>
    public IReadOnlyList<MonthlyRegistrationDto> RegistrationTrend { get; set; } = Array.Empty<MonthlyRegistrationDto>();

    /// <summary>New enrollments per month (last 6 months, oldest → newest).</summary>
    public IReadOnlyList<MonthlyCountDto> EnrollmentTrend { get; set; } = Array.Empty<MonthlyCountDto>();

    /// <summary>Per-category course + enrollment breakdown.</summary>
    public IReadOnlyList<CategoryReportDto> Categories { get; set; } = Array.Empty<CategoryReportDto>();

    /// <summary>Teachers ranked by total students reached.</summary>
    public IReadOnlyList<TopTeacherReportDto> TopTeachers { get; set; } = Array.Empty<TopTeacherReportDto>();

    /// <summary>Enrolled → started → completed → certified funnel across the whole platform.</summary>
    public CompletionFunnelDto Funnel { get; set; } = new();
}

public class MonthlyRegistrationDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    /// <summary>Short month label, e.g. "Jan" or "Jan '26" when the year rolls over.</summary>
    public string Label { get; set; } = string.Empty;
    public int Students { get; set; }
    public int Teachers { get; set; }
    public int Total { get; set; }
}

public class MonthlyCountDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CategoryReportDto
{
    public string Category { get; set; } = string.Empty;
    public int CourseCount { get; set; }
    public int PublishedCount { get; set; }
    public int EnrollmentCount { get; set; }
    public int CompletionRate { get; set; }   // percent of that category's enrolments completed
}

public class TopTeacherReportDto
{
    public Guid TeacherId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int CourseCount { get; set; }
    public int PublishedCount { get; set; }
    public int TotalStudents { get; set; }
}

public class CompletionFunnelDto
{
    public int Enrolled { get; set; }     // total enrolments
    public int Started { get; set; }      // progress > 0
    public int Completed { get; set; }    // IsCompleted
    public int Certified { get; set; }    // certificates issued
}

// ---------------- CSV export rows ------------------------------------------

public class UserExportRow
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CourseExportRow
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string TeacherEmail { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public int ModuleCount { get; set; }
    public int LessonCount { get; set; }
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
