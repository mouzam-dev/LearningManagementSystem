using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Ratings.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Ratings.Commands;

/// <summary>
/// Upsert a course rating from the calling student. Eligibility: the student
/// must be enrolled in the course (BRD: only enrolled students may rate).
/// Resubmitting overwrites the prior row instead of stacking — a student has
/// exactly one rating per course.
/// </summary>
public record SubmitCourseRatingCommand(Guid CourseId, int Rating, string? Review)
    : IRequest<CourseRatingDto>;

public class SubmitCourseRatingCommandValidator : AbstractValidator<SubmitCourseRatingCommand>
{
    public SubmitCourseRatingCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEqual(Guid.Empty);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Review).MaximumLength(2000);
    }
}

public class SubmitCourseRatingCommandHandler
    : IRequestHandler<SubmitCourseRatingCommand, CourseRatingDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubmitCourseRatingCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CourseRatingDto> Handle(SubmitCourseRatingCommand request, CancellationToken ct)
    {
        var studentId = _currentUser.GetUserId();

        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        var isEnrolled = await _db.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.CourseId == course.Id && e.StudentId == studentId, ct);

        if (!isEnrolled)
        {
            throw new UnauthorizedAccessException("You must be enrolled in this course to rate it.");
        }

        var review = (request.Review ?? string.Empty).Trim();
        var now = DateTime.UtcNow;

        var existing = await _db.CourseRatings
            .FirstOrDefaultAsync(r => r.CourseId == course.Id && r.StudentId == studentId, ct);

        if (existing is null)
        {
            existing = new CourseRating
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                StudentId = studentId,
                Rating = request.Rating,
                Review = review,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.CourseRatings.Add(existing);
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

        return new CourseRatingDto
        {
            Id = existing.Id,
            CourseId = existing.CourseId,
            StudentId = existing.StudentId,
            StudentName = $"{student.FirstName} {student.LastName}".Trim(),
            StudentAvatarUrl = student.ProfilePictureUrl,
            Rating = existing.Rating,
            Review = existing.Review,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = existing.UpdatedAt,
        };
    }
}
