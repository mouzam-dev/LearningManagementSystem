using LMS.Application.Admin.Dtos;
using LMS.Application.Admin.Queries;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Commands;

// ---------------- Moderate (force publish / unpublish) ---------------------

/// <summary>
/// Admin override of a course's published state — used to take a problem
/// course offline (or restore one) regardless of who owns it. Every change
/// is written to the audit log.
/// </summary>
public record SetCourseModeratedCommand(Guid CourseId, bool IsPublished)
    : IRequest<AdminCourseDetailDto>;

public class SetCourseModeratedCommandHandler
    : IRequestHandler<SetCourseModeratedCommand, AdminCourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public SetCourseModeratedCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<AdminCourseDetailDto> Handle(SetCourseModeratedCommand request, CancellationToken cancellationToken)
    {
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

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
                new { course.Title, from = was, to = request.IsPublished }));

            await _db.SaveChangesAsync(cancellationToken);
        }

        return await _mediator.Send(new GetAdminCourseDetailQuery(course.Id), cancellationToken);
    }
}

// ---------------- Delete ----------------------------------------------------

public record DeleteCourseCommand(Guid CourseId) : IRequest<Unit>;

public class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCourseCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        var enrolmentCount = await _db.Enrollments
            .CountAsync(e => e.CourseId == course.Id, cancellationToken);

        // Audit the deletion *before* the row is gone, capturing what was lost.
        _db.AuditLogs.Add(AdminAudit.Entry(
            _currentUser.GetUserId(),
            "course.deleted",
            "Course",
            course.Id,
            new { course.Title, course.Category, course.TeacherId, enrolmentCount }));

        // EF cascade removes modules → lessons, assessments → questions →
        // submissions, enrolments, lesson-progress, and certificates.
        _db.Courses.Remove(course);
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
