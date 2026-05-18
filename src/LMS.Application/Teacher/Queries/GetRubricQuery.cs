using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetRubricQuery(Guid Id) : IRequest<RubricDto>;

public class GetRubricQueryHandler : IRequestHandler<GetRubricQuery, RubricDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetRubricQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RubricDto> Handle(GetRubricQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var rubric = await _db.Rubrics.AsNoTracking()
            .Include(r => r.Criteria.OrderBy(c => c.Order))
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TeacherId == teacherId, cancellationToken)
            ?? throw new KeyNotFoundException($"Rubric {request.Id} was not found.");

        return MapToDto(rubric);
    }

    internal static RubricDto MapToDto(Domain.Entities.Rubric r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        TotalPoints = r.Criteria.Sum(c => c.MaxPoints),
        Criteria = r.Criteria
            .OrderBy(c => c.Order)
            .Select(c => new RubricCriterionDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                MaxPoints = c.MaxPoints,
                Order = c.Order,
            })
            .ToList(),
    };
}
