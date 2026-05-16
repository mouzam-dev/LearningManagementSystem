using FluentValidation;
using LMS.Application.Announcements.Dtos;
using LMS.Application.Common;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Announcements.Commands;

/// <summary>
/// Post an announcement to a course the caller owns. Atomically creates the
/// Announcement row AND fans it out to every enrolled student as an in-app
/// Notification — a single SaveChanges call commits both so a partial write
/// can't leave students out of sync with the announcement list.
/// </summary>
public record CreateAnnouncementCommand(
    Guid CourseId, string Title, string Body, bool IsPinned)
    : IRequest<AnnouncementDto>;

public class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEqual(Guid.Empty);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(8000);
    }
}

public class CreateAnnouncementCommandHandler
    : IRequestHandler<CreateAnnouncementCommand, AnnouncementDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notifications;

    public CreateAnnouncementCommandHandler(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _notifications = notifications;
    }

    public async Task<AnnouncementDto> Handle(CreateAnnouncementCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.GetUserId();

        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        if (course.TeacherId != teacherId)
        {
            throw new UnauthorizedAccessException(
                "You can only post announcements to your own courses.");
        }

        var now = DateTime.UtcNow;
        var announcement = new Announcement
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            AuthorId = teacherId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            IsPinned = request.IsPinned,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Announcements.Add(announcement);

        // Fan out to every active enrollment. Skip the teacher themselves if
        // they happen to be enrolled (e.g. preview) so they don't see their
        // own post in their notifications.
        var studentIds = await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == course.Id && e.StudentId != teacherId)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        await _notifications.FanOutAsync(
            studentIds,
            type: "announcement",
            title: $"{course.Title}: {announcement.Title}",
            message: announcement.Body.Length > 280
                ? announcement.Body[..280] + "…"
                : announcement.Body,
            ct: ct);

        await _db.SaveChangesAsync(ct);

        var author = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == teacherId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstAsync(ct);

        return new AnnouncementDto
        {
            Id = announcement.Id,
            CourseId = course.Id,
            CourseTitle = course.Title,
            AuthorId = teacherId,
            AuthorName = $"{author.FirstName} {author.LastName}".Trim(),
            Title = announcement.Title,
            Body = announcement.Body,
            IsPinned = announcement.IsPinned,
            CreatedAt = announcement.CreatedAt,
            UpdatedAt = announcement.UpdatedAt,
        };
    }
}
