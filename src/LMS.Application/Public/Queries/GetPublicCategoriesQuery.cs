using MediatR;
using Microsoft.EntityFrameworkCore;
using LMS.Application.Common;

namespace LMS.Application.Public.Queries;

/// <summary>
/// Distinct categories across currently-published courses, for category chips on
/// the public homepage. Mirrors <see cref="LMS.Application.Student.Queries.GetCategoriesQuery"/>
/// but lives under Public/ so the anonymous endpoint can stay decoupled from the
/// authenticated Student feature surface.
/// </summary>
public record GetPublicCategoriesQuery() : IRequest<IReadOnlyList<string>>;

public class GetPublicCategoriesQueryHandler
    : IRequestHandler<GetPublicCategoriesQuery, IReadOnlyList<string>>
{
    private readonly IApplicationDbContext _db;

    public GetPublicCategoriesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> Handle(
        GetPublicCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        return await _db.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished && !c.IsArchived && c.Category != string.Empty)
            .Select(c => c.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }
}
