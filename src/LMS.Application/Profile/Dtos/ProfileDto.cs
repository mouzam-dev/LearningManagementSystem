namespace LMS.Application.Profile.Dtos;

/// <summary>The signed-in user's own editable profile.</summary>
public class ProfileDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}
