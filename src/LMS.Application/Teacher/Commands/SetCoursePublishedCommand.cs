using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

public record SetCoursePublishedCommand(Guid CourseId, bool IsPublished)
    : IRequest<TeacherCourseDetailDto>;

public class SetCoursePublishedCommandHandler
    : IRequestHandler<SetCoursePublishedCommand, TeacherCourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public SetCoursePublishedCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<TeacherCourseDetailDto> Handle(SetCoursePublishedCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId
                && (c.TeacherId == teacherId
                    || c.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        // Quality gate: must have at least one published lesson before going public.
        // Unpublish is always allowed.
        if (request.IsPublished)
        {
            var publishedLessons = await _db.Lessons
                .CountAsync(l => l.Module.CourseId == course.Id && l.IsPublished, cancellationToken);
            if (publishedLessons == 0)
            {
                throw new InvalidOperationException(
                    "Add at least one published lesson before publishing the course.");
            }
        }

        course.IsPublished = request.IsPublished;
        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new Queries.GetTeacherCourseDetailQuery(course.Id), cancellationToken);
    }
}
