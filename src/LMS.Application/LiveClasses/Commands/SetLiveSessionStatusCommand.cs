using FluentValidation;
using LMS.Application.Attendance.Common;
using LMS.Application.Common;
using LMS.Application.LiveClasses.Common;
using LMS.Application.LiveClasses.Dtos;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.LiveClasses.Commands;

/// <summary>
/// Drives a live session's lifecycle: Start (→ Live, which also opens the linked
/// attendance session), End, or Cancel. Guarded to the course's teacher / co-instructors.
/// </summary>
public record SetLiveSessionStatusCommand(Guid LiveSessionId, string Status) : IRequest<LiveSessionDto>;

public class SetLiveSessionStatusCommandValidator : AbstractValidator<SetLiveSessionStatusCommand>
{
    public SetLiveSessionStatusCommandValidator()
    {
        RuleFor(x => x.LiveSessionId).NotEqual(Guid.Empty);
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<LiveSessionStatus>(s, ignoreCase: true, out _))
            .WithMessage("Status must be Scheduled, Live, Ended, or Cancelled.");
    }
}

public class SetLiveSessionStatusCommandHandler : IRequestHandler<SetLiveSessionStatusCommand, LiveSessionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetLiveSessionStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LiveSessionDto> Handle(SetLiveSessionStatusCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        var live = await _db.LiveSessions.FirstOrDefaultAsync(l => l.Id == request.LiveSessionId, ct)
            ?? throw new KeyNotFoundException($"Live session {request.LiveSessionId} was not found.");

        await AttendanceAuthorization.RequireTeacherCourseAsync(_db, live.CourseId, teacherId, ct);

        var target = Enum.Parse<LiveSessionStatus>(request.Status, ignoreCase: true);
        var now = DateTime.UtcNow;

        switch (target)
        {
            case LiveSessionStatus.Live:
                live.StartedAt ??= now;
                live.Status = LiveSessionStatus.Live;
                // Open the linked attendance session (everyone Absent until they join).
                await LiveAttendanceLink.EnsureSessionAsync(_db, live, ct);
                break;
            case LiveSessionStatus.Ended:
                live.Status = LiveSessionStatus.Ended;
                live.EndedAt = now;
                break;
            case LiveSessionStatus.Cancelled:
                live.Status = LiveSessionStatus.Cancelled;
                break;
            default:
                live.Status = LiveSessionStatus.Scheduled;
                break;
        }

        live.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return await LiveClassMapping.LoadDtoAsync(_db, live.Id, ct);
    }
}
