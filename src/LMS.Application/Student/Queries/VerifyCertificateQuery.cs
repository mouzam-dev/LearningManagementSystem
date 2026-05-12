using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

/// <summary>
/// Public verification — no auth required. Returns a minimal payload that
/// proves the certificate exists without exposing internal ids.
/// </summary>
public record VerifyCertificateQuery(string VerifyCode) : IRequest<VerifyCertificateDto>;

public class VerifyCertificateQueryHandler
    : IRequestHandler<VerifyCertificateQuery, VerifyCertificateDto>
{
    private readonly IApplicationDbContext _db;

    public VerifyCertificateQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<VerifyCertificateDto> Handle(VerifyCertificateQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VerifyCode))
        {
            return new VerifyCertificateDto { IsValid = false, VerifyCode = request.VerifyCode ?? string.Empty };
        }

        var code = request.VerifyCode.Trim().ToUpperInvariant();

        var cert = await _db.Certificates
            .AsNoTracking()
            .Include(c => c.Course).ThenInclude(c => c.Teacher)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.VerifyCode == code, cancellationToken);

        if (cert is null)
        {
            return new VerifyCertificateDto { IsValid = false, VerifyCode = code };
        }

        var completedAt = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == cert.UserId && e.CourseId == cert.CourseId)
            .Select(e => e.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new VerifyCertificateDto
        {
            IsValid = true,
            VerifyCode = cert.VerifyCode,
            CourseTitle = cert.Course.Title,
            StudentName = ($"{cert.User.FirstName} {cert.User.LastName}").Trim(),
            InstructorName = ($"{cert.Course.Teacher.FirstName} {cert.Course.Teacher.LastName}").Trim(),
            IssuedAt = cert.IssuedAt,
            CompletedAt = completedAt,
        };
    }
}
