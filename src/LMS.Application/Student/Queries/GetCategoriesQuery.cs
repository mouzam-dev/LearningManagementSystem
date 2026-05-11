using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

/// <summary>
/// Returns the distinct list of categories on currently-published courses,
/// for populating the catalog filter dropdown.
/// </summary>
public record GetCategoriesQuery() : IRequest<IReadOnlyList<string>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<string>>
{
    private readonly IApplicationDbContext _db;

    public GetCategoriesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<string>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Courses
            .AsNoTracking()
            .Where(c => c.IsPublished && c.Category != string.Empty)
            .Select(c => c.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }
}
