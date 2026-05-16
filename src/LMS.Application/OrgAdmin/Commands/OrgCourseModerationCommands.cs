using LMS.Application.Admin;
using LMS.Application.Common;
using LMS.Application.OrgAdmin.Dtos;
using LMS.Application.OrgAdmin.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.OrgAdmin.Commands;

// ---------------- Moderate (publish / unpublish) ---------------------------

/// <summary>
/// OrgAdmin equivalent of <c>SetCourseModeratedCommand</c>. Identical effect —
/// flip <c>Course.IsPublished</c> and audit — but the lookup is scoped to the
/// caller's organization, so an OrgAdmin can't touch another tenant's courses.
/// </summary>
public record OrgSetCoursePublishedCommand(Guid CourseId, bool IsPublished)
    : IRequest<OrgAdminCourseDetailDto>;

public class OrgSetCoursePublishedCommandHandler
    : IRequestHandler<OrgSetCoursePublishedCommand, OrgAdminCourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public OrgSetCoursePublishedCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<OrgAdminCourseDetailDto> Handle(OrgSetCoursePublishedCommand request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var course = await _db.Courses
            .FirstOrDefaultAsync(
                c => c.Id == request.CourseId && c.OrganizationId == orgId, ct)
            ?? throw new KeyNotFoundException(
                $"Course {request.CourseId} was not found in your organization.");

        var was = course.IsPublished;
        if (was != request.IsPublished)
        {
            course.IsPublished = request.IsPublished;
            course.UpdatedAt = DateTime.UtcNow;

            _db.AuditLogs.Add(AdminAudit.Entry(
                _currentUser.GetUserId(),
                request.IsPublished ? "course.published" : "course.unpublished",
                "Course",
                course.Id,
                new
                {
                    course.Title,
                    organizationId = orgId,
                    from = was,
                    to = request.IsPublished,
                    by = "OrgAdmin",
                }));

            await _db.SaveChangesAsync(ct);
        }

        return await _mediator.Send(new GetOrgCourseDetailQuery(course.Id), ct);
    }
}

// ---------------- Delete ---------------------------------------------------

public record OrgDeleteCourseCommand(Guid CourseId) : IRequest<Unit>;

public class OrgDeleteCourseCommandHandler : IRequestHandler<OrgDeleteCourseCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OrgDeleteCourseCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(OrgDeleteCourseCommand request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var course = await _db.Courses
            .FirstOrDefaultAsync(
                c => c.Id == request.CourseId && c.OrganizationId == orgId, ct)
            ?? throw new KeyNotFoundException(
                $"Course {request.CourseId} was not found in your organization.");

        var enrollmentCount = await _db.Enrollments
            .CountAsync(e => e.CourseId == course.Id, ct);

        _db.AuditLogs.Add(AdminAudit.Entry(
            _currentUser.GetUserId(),
            "course.deleted",
            "Course",
            course.Id,
            new
            {
                course.Title,
                course.Category,
                course.TeacherId,
                organizationId = orgId,
                enrollmentCount,
                by = "OrgAdmin",
            }));

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
