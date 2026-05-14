using LMS.Application.Admin.Dtos;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Queries;

/// <summary>
/// Paginated user list with optional filtering by role, active state, and a
/// case-insensitive search across name + email.
/// </summary>
public record GetAdminUsersQuery(
    string? Search = null,
    string? Role = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 25
) : IRequest<AdminUsersPage>;

public class GetAdminUsersQueryHandler
    : IRequestHandler<GetAdminUsersQuery, AdminUsersPage>
{
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _db;

    public GetAdminUsersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminUsersPage> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role.Trim();
            query = query.Where(u => u.Role == role);
        }

        if (request.IsActive is bool active)
        {
            query = query.Where(u => u.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s)
                || u.FirstName.ToLower().Contains(s)
                || u.LastName.ToLower().Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserListItemDto
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                IsVerified = u.IsVerified,
                CreatedAt = u.CreatedAt,
                CoursesTaught = u.Role == "Teacher"
                    ? _db.Courses.Count(c => c.TeacherId == u.Id)
                    : 0,
                Enrollments = u.Role == "Student"
                    ? u.Enrollments.Count
                    : 0,
                Certificates = u.Certificates.Count,
            })
            .ToListAsync(cancellationToken);

        return new AdminUsersPage
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }
}
