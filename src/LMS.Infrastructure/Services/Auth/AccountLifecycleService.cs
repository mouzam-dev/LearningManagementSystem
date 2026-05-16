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
        var subject = "Verify your LMS email";
        var body = $@"
            <p>Hi {Html(user.FirstName)},</p>
            <p>Confirm your email so your LMS account is fully set up:</p>
            <p><a href=""{url}"">Verify my email</a></p>
            <p>This link expires in {EmailVerificationLifetime.TotalHours:0} hours.
               If you didn't sign up, you can safely ignore this message.</p>";
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
        var subject = "Reset your LMS password";
        var body = $@"
            <p>Hi {Html(user.FirstName)},</p>
            <p>Someone asked to reset the password on your LMS account.
               If that was you, follow the link below to choose a new password:</p>
            <p><a href=""{url}"">Reset my password</a></p>
            <p>This link expires in {PasswordResetLifetime.TotalMinutes:0} minutes.
               If you didn't request this, you can ignore the email — your password
               will stay the same.</p>";
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

    private string WebUrl() => _config["App:PublicWebUrl"]?.TrimEnd('/') ?? "http://localhost:4201";

    /// <summary>Tiny escaper for first-name interpolation into the HTML email.</summary>
    private static string Html(string? value) => (value ?? string.Empty)
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}
