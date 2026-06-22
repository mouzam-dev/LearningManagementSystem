namespace LMS.Application.Public.Dtos;

/// <summary>
/// A teacher who has at least one published course, for the teacher filter on the
/// public course catalog. No contact details — just enough to label and filter.
/// </summary>
public class PublicTeacherDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CourseCount { get; set; }
}
