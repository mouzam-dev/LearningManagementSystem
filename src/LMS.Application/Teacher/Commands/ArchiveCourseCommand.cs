using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Application.Teacher.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

/// <summary>
/// Soft-archive a course. Primary-teacher only. Archived courses disappear
/// from the student catalog and stop accepting new enrollments, but every
/// existing enrollment + the lesson content survives so students who already
/// signed up can finish.
/// </summary>
public record SetCourseArchivedCommand(Guid CourseId, bool IsArchived)
    : IRequest<TeacherCourseDetailDto>;

public class SetCourseArchivedCommandHandler
    : IRequestHandler<SetCourseArchivedCommand, TeacherCourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public SetCourseArchivedCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<TeacherCourseDetailDto> Handle(SetCourseArchivedCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        // Primary-only — archiving is a structural decision for the course owner.
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId && c.TeacherId == teacherId, ct)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        if (course.IsArchived != request.IsArchived)
        {
            course.IsArchived = request.IsArchived;
            course.ArchivedAt = request.IsArchived ? DateTime.UtcNow : null;
            // Archive also unpublishes — keeps the catalog filter simple
            // (one rule: published = visible to students). Unarchiving leaves
            // the course in Draft so the teacher republishes intentionally.
            if (request.IsArchived) course.IsPublished = false;
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return await _mediator.Send(new GetTeacherCourseDetailQuery(course.Id), ct);
    }
}
