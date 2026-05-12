using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

public record GetCertificateQuery(Guid CertificateId) : IRequest<CertificateDto>;

public class GetCertificateQueryHandler : IRequestHandler<GetCertificateQuery, CertificateDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCertificateQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CertificateDto> Handle(GetCertificateQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var cert = await _db.Certificates
            .AsNoTracking()
            .Include(c => c.Course).ThenInclude(c => c.Teacher)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == request.CertificateId, cancellationToken)
            ?? throw new KeyNotFoundException($"Certificate {request.CertificateId} was not found.");

        if (cert.UserId != userId)
        {
            // Cert exists but belongs to someone else — same 404 to avoid id enumeration.
            throw new KeyNotFoundException($"Certificate {request.CertificateId} was not found.");
        }

        var enrollment = await _db.Enrollments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudentId == userId && e.CourseId == cert.CourseId, cancellationToken);

        var totalLessons = await _db.Lessons
            .AsNoTracking()
            .CountAsync(l => l.Module.CourseId == cert.CourseId && l.IsPublished, cancellationToken);

        return new CertificateDto
        {
            CertificateId = cert.Id,
            VerifyCode = cert.VerifyCode,
            IssuedAt = cert.IssuedAt,
            CourseId = cert.CourseId,
            CourseTitle = cert.Course.Title,
            CourseDescription = cert.Course.Description,
            CourseCategory = cert.Course.Category,
            StudentId = cert.UserId,
            StudentName = ($"{cert.User.FirstName} {cert.User.LastName}").Trim(),
            InstructorName = ($"{cert.Course.Teacher.FirstName} {cert.Course.Teacher.LastName}").Trim(),
            EnrolledAt = enrollment?.EnrolledAt ?? cert.IssuedAt,
            CompletedAt = enrollment?.CompletedAt,
            TotalLessons = totalLessons,
        };
    }
}
