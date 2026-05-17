using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

/// <summary>
/// Teachers in the same organization as the course's primary teacher, minus
/// the primary teacher and anyone already listed as a co-instructor — i.e. the
/// shortlist a primary teacher picks from when adding a co-instructor. Only
/// the primary teacher (not co-instructors) can call this.
/// </summary>
public record GetEligibleCoInstructorsQuery(Guid CourseId, string? Search = null)
    : IRequest<IReadOnlyList<EligibleCoInstructorDto>>;

public class GetEligibleCoInstructorsQueryHandler
    : IRequestHandler<GetEligibleCoInstructorsQuery, IReadOnlyList<EligibleCoInstructorDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetEligibleCoInstructorsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<EligibleCoInstructorDto>> Handle(
        GetEligibleCoInstructorsQuery request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        // Primary-only: managing co-instructors is owner-level.
        var course = await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.CourseId && c.TeacherId == teacherId)
            .Select(c => new { c.Id, c.OrganizationId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        var existingIds = await _db.CourseCoInstructors
            .AsNoTracking()
            .Where(ci => ci.CourseId == course.Id)
            .Select(ci => ci.UserId)
            .ToListAsync(ct);
        existingIds.Add(teacherId); // exclude primary too

        var query = _db.Users
            .AsNoTracking()
            .Where(u => u.Role == Roles.Teacher
                     && u.IsActive
                     && u.OrganizationId == course.OrganizationId
                     && !existingIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(s)
                || u.LastName.ToLower().Contains(s)
                || u.Email.ToLower().Contains(s));
        }

        return await query
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Take(50)
            .Select(u => new EligibleCoInstructorDto
            {
                UserId = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                BranchName = u.Branch != null ? u.Branch.Name : null,
            })
            .ToListAsync(ct);
    }
}
