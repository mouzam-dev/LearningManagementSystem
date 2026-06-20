using FluentValidation;
using LMS.Application.Attendance.Common;
using LMS.Application.Attendance.Dtos;
using LMS.Application.Common;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Attendance.Commands;

/// <summary>
/// Opens a new attendance session for a course on a date/slot and snapshots the
/// current enrolled roster into records (defaulting everyone Present — the teacher
/// flips exceptions and saves). Guarded to the course's teacher / co-instructors.
/// </summary>
public record CreateAttendanceSessionCommand(
    Guid CourseId,
    DateOnly SessionDate,
    int Slot,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Topic) : IRequest<SessionRosterDto>;

public class CreateAttendanceSessionCommandValidator : AbstractValidator<CreateAttendanceSessionCommand>
{
    public CreateAttendanceSessionCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEqual(Guid.Empty);
        RuleFor(x => x.SessionDate).NotEqual(default(DateOnly));
        RuleFor(x => x.Slot).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Topic).MaximumLength(200);
        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime!.Value)
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue)
            .WithMessage("End time must be after start time.");
    }
}

public class CreateAttendanceSessionCommandHandler
    : IRequestHandler<CreateAttendanceSessionCommand, SessionRosterDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateAttendanceSessionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SessionRosterDto> Handle(CreateAttendanceSessionCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();
        var course = await AttendanceAuthorization.RequireTeacherCourseAsync(_db, request.CourseId, teacherId, ct);

        var clash = await _db.AttendanceSessions.AnyAsync(
            s => s.CourseId == course.Id && s.SessionDate == request.SessionDate && s.Slot == request.Slot, ct);
        if (clash)
        {
            throw new InvalidOperationException(
                $"An attendance session already exists for {request.SessionDate:yyyy-MM-dd}, slot {request.Slot}.");
        }

        var now = DateTime.UtcNow;
        var session = new AttendanceSession
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            OrganizationId = course.OrganizationId,
            BranchId = course.BranchId,
            TakenByTeacherId = teacherId,
            SessionDate = request.SessionDate,
            Slot = request.Slot,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Topic = string.IsNullOrWhiteSpace(request.Topic) ? null : request.Topic.Trim(),
            Status = AttendanceSessionStatus.Open,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.AttendanceSessions.Add(session);

        // Snapshot the roster from current enrolments — immutable history.
        var studentIds = await _db.Enrollments.AsNoTracking()
            .Where(en => en.CourseId == course.Id)
            .Select(en => en.StudentId)
            .ToListAsync(ct);

        foreach (var sid in studentIds)
        {
            _db.AttendanceRecords.Add(new AttendanceRecord
            {
                Id = Guid.NewGuid(),
                AttendanceSessionId = session.Id,
                StudentId = sid,
                CourseId = course.Id,
                BranchId = course.BranchId,
                SessionDate = request.SessionDate,
                Status = AttendanceStatus.Present,
                MarkedByTeacherId = teacherId,
                MarkedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await _db.SaveChangesAsync(ct);
        return await AttendanceMapping.LoadRosterAsync(_db, session.Id, ct);
    }
}
