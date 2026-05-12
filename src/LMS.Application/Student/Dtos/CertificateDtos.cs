namespace LMS.Application.Student.Dtos;

/// <summary>One row in the student's earned-certificates list.</summary>
public class CertificateSummaryDto
{
    public Guid CertificateId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? CourseCategory { get; set; }
    public string VerifyCode { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>Full payload for the printable certificate view.</summary>
public class CertificateDto
{
    public Guid CertificateId { get; set; }
    public string VerifyCode { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }

    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? CourseDescription { get; set; }
    public string? CourseCategory { get; set; }

    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;

    public string InstructorName { get; set; } = string.Empty;

    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalLessons { get; set; }
}

/// <summary>Public verification payload — minimal personal info exposed.</summary>
public class VerifyCertificateDto
{
    public bool IsValid { get; set; }
    public string VerifyCode { get; set; } = string.Empty;
    public string? CourseTitle { get; set; }
    public string? StudentName { get; set; }
    public string? InstructorName { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
