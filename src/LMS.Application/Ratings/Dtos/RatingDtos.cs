namespace LMS.Application.Ratings.Dtos;

public class CourseRatingDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentAvatarUrl { get; set; }
    public int Rating { get; set; }
    public string Review { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TeacherRatingDto
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? StudentAvatarUrl { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Review { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Aggregated stats for a target (course or teacher): average, total count and
/// a per-star breakdown so the UI can render a 1-5 histogram without a second
/// round-trip. Returned alongside the paginated review list.
/// </summary>
public class RatingSummaryDto
{
    public double Average { get; set; }
    public int TotalCount { get; set; }
    public int OneStar { get; set; }
    public int TwoStar { get; set; }
    public int ThreeStar { get; set; }
    public int FourStar { get; set; }
    public int FiveStar { get; set; }
}

public class CourseRatingsPageDto
{
    public RatingSummaryDto Summary { get; set; } = new();
    public IReadOnlyList<CourseRatingDto> Items { get; set; } = Array.Empty<CourseRatingDto>();
    /// <summary>The caller's own rating, if they've left one. null otherwise.</summary>
    public CourseRatingDto? MyRating { get; set; }
}

public class TeacherRatingsPageDto
{
    public RatingSummaryDto Summary { get; set; } = new();
    public IReadOnlyList<TeacherRatingDto> Items { get; set; } = Array.Empty<TeacherRatingDto>();
}
