using FluentValidation;
using LMS.Application.Attendance.Common;
using LMS.Application.Attendance.Dtos;
using LMS.Application.Common;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Attendance.Commands;

/// <summary>
/// Bulk-saves status changes for an open session (the "save the grid" action).
/// Marks for students not on the snapshotted roster, or with an unrecognized
/// status, are skipped. Rejected if the session is Finalized/Cancelled.
/// </summary>
public record MarkAttendanceCommand(Guid SessionId, List<MarkInputDto> Marks)
    : IRequest<SessionRosterDto>;

public class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    public MarkAttendanceCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEqual(Guid.Empty);
        RuleFor(x => x.Marks).NotNull();
        RuleForEach(x => x.Marks).ChildRules(m =>
        {
            m.RuleFor(x => x.StudentId).NotEqual(Guid.Empty);
            m.RuleFor(x => x.Status)
                .Must(s => Enum.TryParse<AttendanceStatus>(s, ignoreCase: true, out _))
                .WithMessage("Unknown attendance status.");
            m.RuleFor(x => x.Remark).MaximumLength(500);
            m.RuleFor(x => x.MinutesLate).InclusiveBetween(0, 600).When(x => x.MinutesLate.HasValue);
        });
    }
}

public class MarkAttendanceCommandHandler
    : IRequestHandler<MarkAttendanceCommand, SessionRosterDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public MarkAttendanceCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SessionRosterDto> Handle(MarkAttendanceCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        var session = await _db.AttendanceSessions
            .Include(s => s.Records)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, ct)
            ?? throw new KeyNotFoundException($"Attendance session {request.SessionId} was not found.");

        await AttendanceAuthorization.RequireTeacherCourseAsync(_db, session.CourseId, teacherId, ct);

        if (session.Status != AttendanceSessionStatus.Open)
        {
            throw new InvalidOperationException("This session is locked. Re-open it before editing.");
        }

        var now = DateTime.UtcNow;
        var byStudent = session.Records.ToDictionary(r => r.StudentId);

        foreach (var mark in request.Marks)
        {
            if (!byStudent.TryGetValue(mark.StudentId, out var rec)) continue;
            if (!Enum.TryParse<AttendanceStatus>(mark.Status, ignoreCase: true, out var status)) continue;

            rec.Status = status;
            rec.MinutesLate = status == AttendanceStatus.Late ? mark.MinutesLate : null;
            rec.Remark = string.IsNullOrWhiteSpace(mark.Remark) ? null : mark.Remark.Trim();
            rec.MarkedByTeacherId = teacherId;
            rec.MarkedAt = now;
            rec.UpdatedAt = now;
        }

        session.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return await AttendanceMapping.LoadRosterAsync(_db, session.Id, ct);
    }
}
