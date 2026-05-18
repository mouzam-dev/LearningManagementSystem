namespace LMS.Application.Admin.Dtos;

/// <summary>One CSV row as parsed by the controller and handed to the handler.</summary>
public class BulkImportUserRow
{
    /// <summary>1-based line number in the original CSV (header is line 1, first data row is line 2).</summary>
    public int Line { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    /// <summary>Optional. Empty -> Default org (the seeded fallback).</summary>
    public string OrganizationSlug { get; set; } = string.Empty;
    /// <summary>Optional. Empty -> the org's HQ branch.</summary>
    public string BranchName { get; set; } = string.Empty;
}

public class BulkImportRowResultDto
{
    public int Line { get; set; }
    public string Email { get; set; } = string.Empty;
    /// <summary>"Created" or "Failed".</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>New user's Id when Status=Created; null otherwise.</summary>
    public Guid? UserId { get; set; }
    /// <summary>Human-readable failure reason when Status=Failed; null otherwise.</summary>
    public string? Error { get; set; }
}

public class BulkImportUsersResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public IReadOnlyList<BulkImportRowResultDto> Rows { get; set; } = Array.Empty<BulkImportRowResultDto>();
}
