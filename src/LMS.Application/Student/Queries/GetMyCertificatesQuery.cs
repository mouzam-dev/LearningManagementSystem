using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

public record GetMyCertificatesQuery() : IRequest<IReadOnlyList<CertificateSummaryDto>>;

public class GetMyCertificatesQueryHandler
    : IRequestHandler<GetMyCertificatesQuery, IReadOnlyList<CertificateSummaryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyCertificatesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CertificateSummaryDto>> Handle(
        GetMyCertificatesQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        return await _db.Certificates
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new CertificateSummaryDto
            {
                CertificateId = c.Id,
                CourseId = c.CourseId,
                CourseTitle = c.Course.Title,
                CourseCategory = c.Course.Category,
                VerifyCode = c.VerifyCode,
                IssuedAt = c.IssuedAt,
                CompletedAt = _db.Enrollments
                    .Where(e => e.StudentId == userId && e.CourseId == c.CourseId)
                    .Select(e => e.CompletedAt)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
    }
}
