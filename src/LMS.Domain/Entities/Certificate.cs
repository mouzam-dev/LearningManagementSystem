namespace LMS.Domain.Entities;

public class Certificate
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string VerifyCode { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }

    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
