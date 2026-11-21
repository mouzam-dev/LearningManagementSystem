using System.Security.Cryptography;
using System.Text;
using LMS.Application.Auth;
using LMS.Application.Common;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LMS.Infrastructure.Services.Auth;

/// <summary>
/// Owns the issue + consume halves of email-verification and password-reset
/// flows. The raw token is the only thing ever sent over the wire; we persist
/// only a SHA-256 hash so a DB leak can't impersonate users.
/// </summary>
public class AccountLifecycleService : IAccountLifecycleService
{
    // 32 bytes -> 43-char base64url, ~256 bits of entropy. Plenty for a
    // one-shot URL token that expires in a day.
    private const int TokenByteLength = 32;
    private static readonly TimeSpan EmailVerificationLifetime = TimeSpan.FromDays(2);
    private static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromHours(1);

    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;

    public AccountLifecycleService(
        IApplicationDbContext db,
        IEmailSender email,
        IPasswordHasher hasher,
        IConfiguration config)
    {
        _db = db;
        _email = email;
        _hasher = hasher;
        _config = config;
    }

    // -------------------------------------------------------------------
    // Email verification
    // -------------------------------------------------------------------

    public async Task IssueEmailVerificationAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || user.IsVerified) return; // no-op for unknown / already-verified

        var raw = NewRawToken();
        _db.UserTokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(raw),
            Purpose = UserTokenPurposes.EmailVerification,
            ExpiresAt = DateTime.UtcNow.Add(EmailVerificationLifetime),
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        var url = $"{WebUrl()}/auth/verify-email?token={Uri.EscapeDataString(raw)}";
        var subject = $"Verify your {SiteName} email";
        var body = Layout(user.FirstName,
            $"Please confirm your email address so your {SiteName} account is fully set up.",
            "Verify my email", url,
            $"This link expires in {EmailVerificationLifetime.TotalHours:0} hours. If you didn't sign up, you can safely ignore this email.");
        await _email.SendAsync(user.Email, subject, body, ct);
    }

    public async Task<bool> ConsumeEmailVerificationAsync(string rawToken, CancellationToken ct = default)
    {
        var (row, user) = await LoadValidTokenAsync(rawToken, UserTokenPurposes.EmailVerification, ct);
        if (row is null || user is null) return false;

        row.ConsumedAt = DateTime.UtcNow;
        if (!user.IsVerified)
        {
            user.IsVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // -------------------------------------------------------------------
    // Password reset
    // -------------------------------------------------------------------

    public async Task IssuePasswordResetAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized, ct);
        if (user is null)
        {
            // Don't reveal that the address isn't registered — silently no-op.
            return;
        }

        var raw = NewRawToken();
        _db.UserTokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(raw),
            Purpose = UserTokenPurposes.PasswordReset,
            ExpiresAt = DateTime.UtcNow.Add(PasswordResetLifetime),
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        var url = $"{WebUrl()}/auth/reset-password?token={Uri.EscapeDataString(raw)}";
        var subject = $"Reset your {SiteName} password";
        var body = Layout(user.FirstName,
            $"We received a request to reset the password for your {SiteName} account. If that was you, choose a new password below.",
            "Reset my password", url,
            $"This link expires in {PasswordResetLifetime.TotalMinutes:0} minutes and can be used once. If you didn't ask for this, ignore this email and your password will stay the same.");
        await _email.SendAsync(user.Email, subject, body, ct);
    }

    public async Task<bool> ConsumePasswordResetAsync(string rawToken, string newPassword, CancellationToken ct = default)
    {
        var (row, user) = await LoadValidTokenAsync(rawToken, UserTokenPurposes.PasswordReset, ct);
        if (row is null || user is null) return false;

        user.PasswordHash = _hasher.Hash(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        row.ConsumedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    /// <summary>Load + validate a token: must exist, match purpose, not be expired, not be consumed.</summary>
    private async Task<(UserToken? Row, User? User)> LoadValidTokenAsync(
        string rawToken, string purpose, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return (null, null);

        var hash = Hash(rawToken);
        var row = await _db.UserTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.Purpose == purpose, ct);
        if (row is null) return (null, null);
        if (row.ConsumedAt is not null) return (null, null);
        if (row.ExpiresAt <= DateTime.UtcNow) return (null, null);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == row.UserId, ct);
        if (user is null) return (null, null);

        return (row, user);
    }

    private static string NewRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        // URL-safe base64 so the value drops into a querystring without escaping.
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string Hash(string raw)
    {
        var bytes = Encoding.UTF8.GetBytes(raw);
        var digest = SHA256.HashData(bytes);
        return Convert.ToHexString(digest); // upper-case hex, 64 chars
    }

    private const string SiteName = "Daura-e-Ilmiyah Shariyah";

    /// <summary>Branded HTML email: inline styles only, since email clients drop &lt;style&gt; blocks.</summary>
    private static string Layout(string firstName, string intro, string button, string url, string note) => $@"
<div style=""background:#f6f7f8;padding:24px 12px;font-family:Arial,Helvetica,sans-serif;color:#2c3540"">
  <div style=""max-width:520px;margin:0 auto;background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e5e7eb"">
    <div style=""background:#1f6438;color:#ffffff;padding:18px 24px;font-size:18px;font-weight:bold"">{SiteName}</div>
    <div style=""padding:24px;font-size:15px;line-height:1.6"">
      <p style=""margin:0 0 12px"">Assalamu alaikum {Html(firstName)},</p>
      <p style=""margin:0 0 20px"">{Html(intro)}</p>
      <p style=""margin:0 0 20px""><a href=""{url}"" style=""display:inline-block;background:#a2761c;color:#ffffff;text-decoration:none;font-weight:bold;padding:12px 22px;border-radius:10px"">{button}</a></p>
      <p style=""margin:0 0 12px;font-size:13px;color:#6b7280"">{Html(note)}</p>
      <p style=""margin:0;font-size:12px;color:#9ca3af;word-break:break-all"">If the button doesn't work, copy this link into your browser:<br>{url}</p>
    </div>
  </div>
</div>";

    private string WebUrl() => _config["App:PublicWebUrl"]?.TrimEnd('/') ?? "http://localhost:4201";

    /// <summary>Tiny escaper for first-name interpolation into the HTML email.</summary>
    private static string Html(string? value) => (value ?? string.Empty)
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}
