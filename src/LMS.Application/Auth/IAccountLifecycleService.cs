namespace LMS.Application.Auth;

/// <summary>
/// Issues + verifies single-use account-lifecycle tokens (email verification,
/// password reset) and sends the matching email. Lives in Application so
/// AuthController can stay slim; the SMTP transport is in Infrastructure
/// behind <c>IEmailSender</c>.
/// </summary>
public interface IAccountLifecycleService
{
    /// <summary>
    /// Issue an email-verification token for the user and email them the link.
    /// Safe to call repeatedly; an outstanding token is invalidated by issuing
    /// a fresh one — verification consumes ANY non-expired token, so the
    /// latest one wins in practice.
    /// </summary>
    Task IssueEmailVerificationAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Consume a token and mark the user verified. Returns <c>false</c> when
    /// the token is unknown, expired, or already consumed.
    /// </summary>
    Task<bool> ConsumeEmailVerificationAsync(string rawToken, CancellationToken ct = default);

    /// <summary>
    /// Look up the user by email and email them a password-reset link. Does
    /// NOT reveal whether the email exists in the system — silently succeeds
    /// regardless to prevent account enumeration.
    /// </summary>
    Task IssuePasswordResetAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Consume a reset token, hash the new password, persist it. Returns
    /// <c>false</c> for unknown/expired/consumed tokens.
    /// </summary>
    Task<bool> ConsumePasswordResetAsync(string rawToken, string newPassword, CancellationToken ct = default);
}
