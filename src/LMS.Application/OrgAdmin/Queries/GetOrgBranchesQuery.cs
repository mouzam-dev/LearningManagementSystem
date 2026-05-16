using LMS.Application.Common;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.OrgAdmin.Queries;

public record GetOrgBranchesQuery() : IRequest<IReadOnlyList<BranchListItemDto>>;

public class GetOrgBranchesQueryHandler
    : IRequestHandler<GetOrgBranchesQuery, IReadOnlyList<BranchListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrgBranchesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<BranchListItemDto>> Handle(
        GetOrgBranchesQuery request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        return await _db.Branches
            .AsNoTracking()
            .Where(b => b.OrganizationId == orgId)
            .OrderBy(b => b.Name)
            .Select(b => new BranchListItemDto
            {
                Id = b.Id,
                OrganizationId = b.OrganizationId,
                Name = b.Name,
                Code = b.Code,
                Location = b.Location,
                ContactEmail = b.ContactEmail,
                IsActive = b.IsActive,
                TeacherCount = b.Users.Count(u => u.Role == Roles.Teacher),
                StudentCount = b.Users.Count(u => u.Role == Roles.Student),
                CreatedAt = b.CreatedAt,
            })
            .ToListAsync(ct);
    }
}
