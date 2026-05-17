using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Application.Teacher.Queries;
using LMS.Domain.Constants;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

// ---------------- Add co-instructor -----------------------------------------

/// <summary>
/// Add another teacher as a co-instructor on a course. Primary-teacher only.
/// The candidate must be an active Teacher in the same organization as the
/// course and not already an instructor. Returns the refreshed roster.
/// </summary>
public record AddCourseCoInstructorCommand(Guid CourseId, Guid UserId)
    : IRequest<IReadOnlyList<CourseInstructorDto>>;

public class AddCourseCoInstructorCommandValidator : AbstractValidator<AddCourseCoInstructorCommand>
{
    public AddCourseCoInstructorCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEqual(Guid.Empty);
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
    }
}

public class AddCourseCoInstructorCommandHandler
    : IRequestHandler<AddCourseCoInstructorCommand, IReadOnlyList<CourseInstructorDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public AddCourseCoInstructorCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<IReadOnlyList<CourseInstructorDto>> Handle(
        AddCourseCoInstructorCommand request, CancellationToken ct)
    {
        var actorId = _currentUser.GetUserId();

        // Primary-only — co-instructors can't add more co-instructors.
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.TeacherId == actorId, ct)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        if (request.UserId == course.TeacherId)
        {
            throw new InvalidOperationException(
                "The primary teacher is already an instructor on this course.");
        }

        var candidate = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("That user does not exist.");

        if (candidate.Role != Roles.Teacher || !candidate.IsActive)
        {
            throw new InvalidOperationException(
                "Co-instructors must be active Teacher accounts.");
        }

        if (candidate.OrganizationId != course.OrganizationId)
        {
            throw new InvalidOperationException(
                "Co-instructors must belong to the same organization as the course.");
        }

        var alreadyOnIt = await _db.CourseCoInstructors
            .AnyAsync(ci => ci.CourseId == course.Id && ci.UserId == candidate.Id, ct);
        if (alreadyOnIt)
        {
            throw new InvalidOperationException(
                "That teacher is already a co-instructor on this course.");
        }

        _db.CourseCoInstructors.Add(new CourseCoInstructor
        {
            CourseId = course.Id,
            UserId = candidate.Id,
            AddedByUserId = actorId,
            AddedAt = DateTime.UtcNow,
        });
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await _mediator.Send(new GetCourseInstructorsQuery(course.Id), ct);
    }
}

// ---------------- Remove co-instructor --------------------------------------

public record RemoveCourseCoInstructorCommand(Guid CourseId, Guid UserId)
    : IRequest<IReadOnlyList<CourseInstructorDto>>;

public class RemoveCourseCoInstructorCommandHandler
    : IRequestHandler<RemoveCourseCoInstructorCommand, IReadOnlyList<CourseInstructorDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public RemoveCourseCoInstructorCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<IReadOnlyList<CourseInstructorDto>> Handle(
        RemoveCourseCoInstructorCommand request, CancellationToken ct)
    {
        var actorId = _currentUser.GetUserId();

        // Either the primary teacher (managing the roster) OR the
        // co-instructor themselves (resigning) can call this.
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId
                && (c.TeacherId == actorId
                    || (c.CoInstructors.Any(ci => ci.UserId == actorId) && actorId == request.UserId)), ct)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        if (request.UserId == course.TeacherId)
        {
            throw new InvalidOperationException(
                "The primary teacher cannot be removed via this endpoint — transfer the course instead.");
        }

        var link = await _db.CourseCoInstructors
            .FirstOrDefaultAsync(ci => ci.CourseId == course.Id && ci.UserId == request.UserId, ct);
        if (link is null)
        {
            // Idempotent: already gone.
            return await _mediator.Send(new GetCourseInstructorsQuery(course.Id), ct);
        }

        _db.CourseCoInstructors.Remove(link);
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await _mediator.Send(new GetCourseInstructorsQuery(course.Id), ct);
    }
}
