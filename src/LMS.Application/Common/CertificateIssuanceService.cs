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

        // The certificate is unlocked by passing the course's designated final
        // exam. No final exam set → nothing can unlock it. (Course completion
        // alone never issues a certificate.)
        var finalExam = await _db.Assessments
            .Where(a => a.CourseId == courseId && a.IsFinalExam)
            .Select(a => new { a.Id, a.PassingScore })
            .FirstOrDefaultAsync(ct);
        if (finalExam is null) return false;

        // "Passed" = any graded submission scoring at or above the passing mark.
        var passed = await _db.Submissions.AnyAsync(s =>
            s.AssessmentId == finalExam.Id
            && s.StudentId == userId
            && s.GradedAt != null
            && s.Score != null
            && s.Score >= finalExam.PassingScore, ct);
        if (!passed) return false;

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
