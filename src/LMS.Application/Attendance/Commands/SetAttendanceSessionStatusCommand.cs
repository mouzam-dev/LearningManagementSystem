using FluentValidation;
using LMS.Application.Attendance.Common;
using LMS.Application.Attendance.Dtos;
using LMS.Application.Common;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Attendance.Commands;

/// <summary>
/// Transitions a session's lifecycle: Finalize (lock), re-Open (unlock for edits),
/// or Cancel (class didn't happen). Guarded to the course's teacher / co-instructors.
/// </summary>
public record SetAttendanceSessionStatusCommand(Guid SessionId, string Status)
    : IRequest<AttendanceSessionDto>;

public class SetAttendanceSessionStatusCommandValidator
    : AbstractValidator<SetAttendanceSessionStatusCommand>
{
    public SetAttendanceSessionStatusCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEqual(Guid.Empty);
        RuleFor(x => x.Status)
            .Must(s => Enum.TryParse<AttendanceSessionStatus>(s, ignoreCase: true, out _))
            .WithMessage("Status must be Open, Finalized, or Cancelled.");
    }
}

public class SetAttendanceSessionStatusCommandHandler
    : IRequestHandler<SetAttendanceSessionStatusCommand, AttendanceSessionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetAttendanceSessionStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AttendanceSessionDto> Handle(SetAttendanceSessionStatusCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        var session = await _db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, ct)
            ?? throw new KeyNotFoundException($"Attendance session {request.SessionId} was not found.");

        await AttendanceAuthorization.RequireTeacherCourseAsync(_db, session.CourseId, teacherId, ct);

        var target = Enum.Parse<AttendanceSessionStatus>(request.Status, ignoreCase: true);
        var now = DateTime.UtcNow;

        session.Status = target;
        session.FinalizedAt = target == AttendanceSessionStatus.Finalized ? now : null;
        session.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return await AttendanceMapping.LoadSessionDtoAsync(_db, session.Id, ct);
    }
}
