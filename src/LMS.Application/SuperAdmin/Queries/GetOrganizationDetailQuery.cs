using LMS.Application.Common;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.SuperAdmin.Queries;

public record GetOrganizationDetailQuery(Guid OrganizationId) : IRequest<OrganizationDetailDto>;

public class GetOrganizationDetailQueryHandler
    : IRequestHandler<GetOrganizationDetailQuery, OrganizationDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetOrganizationDetailQueryHandler(IApplicationDbContext db) { _db = db; }

    public async Task<OrganizationDetailDto> Handle(
        GetOrganizationDetailQuery request, CancellationToken ct)
    {
        var org = await _db.Organizations
            .AsNoTracking()
            .Where(o => o.Id == request.OrganizationId)
            .Select(o => new OrganizationDetailDto
            {
                Id = o.Id,
                Name = o.Name,
                Slug = o.Slug,
                Description = o.Description,
                ContactEmail = o.ContactEmail,
                LogoUrl = o.LogoUrl,
                IsActive = o.IsActive,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                BranchCount = o.Branches.Count,
                OrgAdminCount = o.Users.Count(u => u.Role == Roles.OrgAdmin),
                TeacherCount = o.Users.Count(u => u.Role == Roles.Teacher),
                StudentCount = o.Users.Count(u => u.Role == Roles.Student),
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Organization {request.OrganizationId} was not found.");

        org.Branches = await _db.Branches
            .AsNoTracking()
            .Where(b => b.OrganizationId == request.OrganizationId)
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

        org.OrgAdmins = await _db.Users
            .AsNoTracking()
            .Where(u => u.OrganizationId == request.OrganizationId && u.Role == Roles.OrgAdmin)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new OrgAdminListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync(ct);

        return org;
    }
}
