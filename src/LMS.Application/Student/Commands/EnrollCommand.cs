using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Commands;

public record EnrollCommand(Guid CourseId) : IRequest<CourseListItemDto>;

public class EnrollCommandHandler : IRequestHandler<EnrollCommand, CourseListItemDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public EnrollCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CourseListItemDto> Handle(EnrollCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var course = await _db.Courses
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        if (!course.IsPublished)
        {
            throw new InvalidOperationException("This course is not currently available for enrollment.");
        }

        var existing = course.Enrollments.FirstOrDefault(e => e.StudentId == userId);
        if (existing is null)
        {
            if (course.MaxStudents is int max && course.Enrollments.Count >= max)
            {
                throw new InvalidOperationException("This course is already full.");
            }

            _db.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = userId,
                CourseId = course.Id,
                EnrolledAt = DateTime.UtcNow,
                ProgressPercentage = 0m,
                IsCompleted = false,
            });

            await _db.SaveChangesAsync(cancellationToken);
        }

        // course.Enrollments is updated via EF change-tracker fixup when the new
        // row is added (CourseId matches a tracked entity), so the count is live.
        return new CourseListItemDto
        {
            CourseId = course.Id,
            Title = course.Title,
            Description = course.Description,
            Category = course.Category,
            ThumbnailUrl = course.ThumbnailUrl,
            TeacherName = $"{course.Teacher.FirstName} {course.Teacher.LastName}".Trim(),
            EnrolledCount = course.Enrollments.Count,
            MaxStudents = course.MaxStudents,
            IsEnrolled = true,
        };
    }
}
