using FluentValidation;
using LMS.Application.Common;
using LMS.Application.LiveClasses.Common;
using LMS.Application.LiveClasses.Dtos;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.LiveClasses.Commands;

/// <summary>
/// A student joins a Live session: validates they're enrolled and the class is live,
/// auto-marks them Present in the linked attendance session, and returns everything
/// the front-end needs to embed the video room.
/// </summary>
public record JoinLiveSessionCommand(Guid LiveSessionId) : IRequest<LiveJoinInfoDto>;

public class JoinLiveSessionCommandValidator : AbstractValidator<JoinLiveSessionCommand>
{
    public JoinLiveSessionCommandValidator()
    {
        RuleFor(x => x.LiveSessionId).NotEqual(Guid.Empty);
    }
}

public class JoinLiveSessionCommandHandler : IRequestHandler<JoinLiveSessionCommand, LiveJoinInfoDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public JoinLiveSessionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<LiveJoinInfoDto> Handle(JoinLiveSessionCommand request, CancellationToken ct)
    {
        var studentId = _currentUser.GetUserId();

        var live = await _db.LiveSessions.FirstOrDefaultAsync(l => l.Id == request.LiveSessionId, ct)
            ?? throw new KeyNotFoundException($"Live session {request.LiveSessionId} was not found.");

        if (live.Status != LiveSessionStatus.Live)
        {
            throw new InvalidOperationException("This class isn't live right now.");
        }

        var enrolled = await _db.Enrollments.AsNoTracking()
            .AnyAsync(e => e.CourseId == live.CourseId && e.StudentId == studentId, ct);
        if (!enrolled)
        {
            throw new UnauthorizedAccessException("You're not enrolled in this course.");
        }

        // Auto-attendance: ensure the linked session exists, mark this student Present.
        var attendance = await LiveAttendanceLink.EnsureSessionAsync(_db, live, ct);
        LiveAttendanceLink.MarkPresent(attendance, studentId, live);
        await _db.SaveChangesAsync(ct);

        var info = await _db.LiveSessions.AsNoTracking()
            .Where(l => l.Id == live.Id)
            .Select(l => new { CourseTitle = l.Course.Title, l.Title, l.Provider, l.RoomName })
            .FirstAsync(ct);

        var student = await _db.Users.AsNoTracking()
            .Where(u => u.Id == studentId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstAsync(ct);

        return new LiveJoinInfoDto
        {
            LiveSessionId = live.Id,
            Provider = info.Provider,
            RoomName = info.RoomName,
            CourseTitle = info.CourseTitle,
            Title = info.Title,
            DisplayName = $"{student.FirstName} {student.LastName}".Trim(),
            Status = live.Status.ToString(),
        };
    }
}
