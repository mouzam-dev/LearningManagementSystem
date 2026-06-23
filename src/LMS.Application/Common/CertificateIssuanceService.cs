using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Common;

/// <summary>
/// Issues a course Certificate of Completion once a student has passed the
/// teacher's exam(s) for the course. This replaces the old "issue as soon as
/// every lesson is watched" rule — finishing the lessons is no longer enough;
/// the student must pass the assessment(s) the teacher set.
/// </summary>
public interface ICertificateIssuanceService
{
    /// <summary>
    /// Issues a certificate for (student, course) when the student has now passed
    /// every assessment in the course and one hasn't already been issued. Returns
    /// true only when a new certificate is created; a no-op (false) when the course
    /// has no assessments, not all are passed yet, or a certificate already exists.
    /// </summary>
    Task<bool> TryIssueForCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);
}

public class CertificateIssuanceService : ICertificateIssuanceService
{
    private readonly IApplicationDbContext _db;

    public CertificateIssuanceService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryIssueForCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        var alreadyIssued = await _db.Certificates
            .AnyAsync(c => c.UserId == userId && c.CourseId == courseId, ct);
        if (alreadyIssued) return false;

        // No exam to pass → no certificate. (Course completion alone never issues one.)
        var totalAssessments = await _db.Assessments
            .CountAsync(a => a.CourseId == courseId, ct);
        if (totalAssessments == 0) return false;

        // An assessment counts as "passed" once the student has any graded
        // submission scoring at or above its passing mark. The certificate is
        // only unlocked when every assessment in the course is passed.
        var passedAssessments = await _db.Assessments
            .Where(a => a.CourseId == courseId)
            .CountAsync(a => a.Submissions.Any(s =>
                s.StudentId == userId
                && s.GradedAt != null
                && s.Score != null
                && s.Score >= a.PassingScore), ct);

        if (passedAssessments < totalAssessments) return false;

        _db.Certificates.Add(new Certificate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = courseId,
            VerifyCode = GenerateVerifyCode(),
            IssuedAt = DateTime.UtcNow,
        });

        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Lost a race against the unique (UserId, CourseId) index — already issued.
            return false;
        }
    }

    /// <summary>
    /// 12-character URL-safe verification code (e.g. "K7J3-N9PQ-XR2M").
    /// Random Guid bytes → Crockford base32 (no ambiguous chars) → dashed groups.
    /// </summary>
    private static string GenerateVerifyCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I/L/O/0/1
        Span<byte> bytes = stackalloc byte[8];
        Random.Shared.NextBytes(bytes);
        Span<char> chars = stackalloc char[12];
        for (var i = 0; i < 12; i++)
        {
            chars[i] = alphabet[bytes[i % 8] % alphabet.Length];
            bytes[i % 8] = (byte)(bytes[i % 8] * 31 + i);
        }
        return $"{new string(chars[..4])}-{new string(chars.Slice(4, 4))}-{new string(chars[8..])}";
    }
}
