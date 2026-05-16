namespace LMS.Domain.Entities;

/// <summary>
/// Single-use, short-lived token used for account-lifecycle flows: email
/// verification and password reset. The raw token is sent to the user via
/// email and never persisted — we store a SHA-256 hash so a database leak
/// can't yield usable tokens.
/// </summary>
public class UserToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>SHA-256 hex of the raw token. The raw value lives only in the email.</summary>
    public string TokenHash { get; set; } = string.Empty;
    /// <summary>"email_verification" or "password_reset" — see <c>UserTokenTypes</c>.</summary>
    public string Purpose { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}

public static class UserTokenPurposes
{
    public const string EmailVerification = "email_verification";
    public const string PasswordReset = "password_reset";
}
