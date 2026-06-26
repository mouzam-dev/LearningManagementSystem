using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetRubricsQuery(string? Search, int Page, int PageSize)
    : IRequest<PagedResult<RubricListItemDto>>;

public class GetRubricsQueryHandler : IRequestHandler<GetRubricsQuery, PagedResult<RubricListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetRubricsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<RubricListItemDto>> Handle(GetRubricsQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _db.Rubrics.AsNoTracking()
            .Where(r => r.TeacherId == teacherId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(r => EF.Functions.Like(r.Name.ToLower(), $"%{s}%"));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RubricListItemDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                CriterionCount = r.Criteria.Count,
                TotalPoints = r.Criteria.Sum(c => c.MaxPoints),
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<RubricListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }
}
