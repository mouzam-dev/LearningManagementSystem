using FluentValidation;
using LMS.Application.Attendance.Common;
using LMS.Application.Common;
using LMS.Application.LiveClasses.Common;
using LMS.Application.LiveClasses.Dtos;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.LiveClasses.Commands;

/// <summary>
/// Schedules a live online class for a course. Guarded to the course's teacher /
/// co-instructors. Generates an unguessable Jitsi room name from the new id.
/// </summary>
public record ScheduleLiveSessionCommand(Guid CourseId, string Title, DateTime ScheduledStart, int DurationMinutes)
    : IRequest<LiveSessionDto>;

public class ScheduleLiveSessionCommandValidator : AbstractValidator<ScheduleLiveSessionCommand>
{
    public ScheduleLiveSessionCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEqual(Guid.Empty);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ScheduledStart).NotEqual(default(DateTime));
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 480);
    }
}

public class ScheduleLiveSessionCommandHandler : IRequestHandler<ScheduleLiveSessionCommand, LiveSessionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ScheduleLiveSessionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LiveSessionDto> Handle(ScheduleLiveSessionCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();
        var course = await AttendanceAuthorization.RequireTeacherCourseAsync(_db, request.CourseId, teacherId, ct);

        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();
        var live = new LiveSession
        {
            Id = id,
            CourseId = course.Id,
            OrganizationId = course.OrganizationId,
            BranchId = course.BranchId,
            HostTeacherId = teacherId,
            Title = request.Title.Trim(),
            ScheduledStart = DateTime.SpecifyKind(request.ScheduledStart, DateTimeKind.Utc),
            DurationMinutes = request.DurationMinutes,
            Status = LiveSessionStatus.Scheduled,
            Provider = "Jitsi",
            RoomName = $"duareshariye-{id:N}",
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.LiveSessions.Add(live);
        await _db.SaveChangesAsync(ct);

        return await LiveClassMapping.LoadDtoAsync(_db, live.Id, ct);
    }
}
