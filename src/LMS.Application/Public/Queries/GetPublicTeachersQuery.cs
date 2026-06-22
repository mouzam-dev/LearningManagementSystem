using LMS.Application.Common;
using LMS.Application.Public.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Public.Queries;

/// <summary>
/// Teachers who currently have at least one published, non-archived course —
/// populates the teacher filter on the public course catalog. Anonymous; no user
/// context required.
/// </summary>
public record GetPublicTeachersQuery() : IRequest<IReadOnlyList<PublicTeacherDto>>;

public class GetPublicTeachersQueryHandler
    : IRequestHandler<GetPublicTeachersQuery, IReadOnlyList<PublicTeacherDto>>
{
    private readonly IApplicationDbContext _db;

    public GetPublicTeachersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PublicTeacherDto>> Handle(
        GetPublicTeachersQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished && !c.IsArchived)
            .GroupBy(c => new { c.TeacherId, c.Teacher.FirstName, c.Teacher.LastName })
            .Select(g => new PublicTeacherDto
            {
                Id = g.Key.TeacherId,
                Name = (g.Key.FirstName + " " + g.Key.LastName).Trim(),
                CourseCount = g.Count(),
            })
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }
}
