using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Ratings.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Ratings.Commands;

/// <summary>
/// Upsert a teacher rating from the calling student. Anchored to a specific
/// course — the student must be enrolled in <c>CourseId</c> AND that course
/// must be taught by <c>TeacherId</c> (primary teacher or co-instructor).
/// Letting the rating differ per course is intentional: a teacher's style
/// can land differently across their catalogue.
/// </summary>
public record SubmitTeacherRatingCommand(
    Guid TeacherId, Guid CourseId, int Rating, string? Review)
    : IRequest<TeacherRatingDto>;

public class SubmitTeacherRatingCommandValidator : AbstractValidator<SubmitTeacherRatingCommand>
{
    public SubmitTeacherRatingCommandValidator()
    {
        RuleFor(x => x.TeacherId).NotEqual(Guid.Empty);
        RuleFor(x => x.CourseId).NotEqual(Guid.Empty);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Review).MaximumLength(2000);
    }
}

public class SubmitTeacherRatingCommandHandler
    : IRequestHandler<SubmitTeacherRatingCommand, TeacherRatingDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubmitTeacherRatingCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherRatingDto> Handle(SubmitTeacherRatingCommand request, CancellationToken ct)
    {
        var studentId = _currentUser.GetUserId();

        var course = await _db.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.CourseId)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.TeacherId,
                IsCoInstructor = c.CoInstructors.Any(ci => ci.UserId == request.TeacherId),
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        if (course.TeacherId != request.TeacherId && !course.IsCoInstructor)
        {
            throw new InvalidOperationException(
                "That teacher doesn't teach the course you're rating them on.");
        }

        var isEnrolled = await _db.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.CourseId == course.Id && e.StudentId == studentId, ct);

        if (!isEnrolled)
        {
            throw new UnauthorizedAccessException(
                "You must be enrolled in this teacher's course to rate them.");
        }

        var review = (request.Review ?? string.Empty).Trim();
        var now = DateTime.UtcNow;

        var existing = await _db.TeacherRatings
            .FirstOrDefaultAsync(r =>
                r.TeacherId == request.TeacherId
                && r.StudentId == studentId
                && r.CourseId == course.Id, ct);

        if (existing is null)
        {
            existing = new TeacherRating
            {
                Id = Guid.NewGuid(),
                TeacherId = request.TeacherId,
                StudentId = studentId,
                CourseId = course.Id,
                Rating = request.Rating,
                Review = review,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.TeacherRatings.Add(existing);
        }
        else
        {
            existing.Rating = request.Rating;
            existing.Review = review;
            existing.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        var student = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == studentId)
            .Select(u => new { u.FirstName, u.LastName, u.ProfilePictureUrl })
            .FirstAsync(ct);

        return new TeacherRatingDto
        {
            Id = existing.Id,
            TeacherId = existing.TeacherId,
            StudentId = existing.StudentId,
            StudentName = $"{student.FirstName} {student.LastName}".Trim(),
            StudentAvatarUrl = student.ProfilePictureUrl,
            CourseId = existing.CourseId,
            CourseTitle = course.Title,
            Rating = existing.Rating,
            Review = existing.Review,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt,
        };
    }
}
