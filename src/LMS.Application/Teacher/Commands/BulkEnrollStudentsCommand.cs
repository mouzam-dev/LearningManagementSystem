using FluentValidation;
using LMS.Application.Admin;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

/// <summary>
/// Bulk-enroll students into a course by email (BRD TCH-043). Each row is
/// resolved independently; one bad email never blocks the rest. Rows where
/// the student is already enrolled return Skipped (not Failed) so re-uploading
/// the same CSV is idempotent.
/// </summary>
public record BulkEnrollStudentsCommand(Guid CourseId, IReadOnlyList<BulkEnrollRow> Rows)
    : IRequest<BulkEnrollResultDto>;

public class BulkEnrollStudentsCommandValidator : AbstractValidator<BulkEnrollStudentsCommand>
{
    public BulkEnrollStudentsCommandValidator()
    {
        RuleFor(x => x.Rows).NotNull()
            .Must(r => r.Count >= 1).WithMessage("CSV had no data rows after the header.")
            .Must(r => r.Count <= 1000).WithMessage("Enroll up to 1000 students at a time.");
    }
}

public class BulkEnrollStudentsCommandHandler
    : IRequestHandler<BulkEnrollStudentsCommand, BulkEnrollResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BulkEnrollStudentsCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BulkEnrollResultDto> Handle(
        BulkEnrollStudentsCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.GetUserId();

        // Auth: only the course owner or a co-instructor can bulk-enroll.
        // Archived courses are blocked — enrollment into a frozen course is
        // surprising. Unpublished is allowed (pre-seeding before launch).
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId
                && !c.IsArchived
                && (c.TeacherId == actorId
                    || c.CoInstructors.Any(ci => ci.UserId == actorId)),
                cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        var currentEnrollmentCount = await _db.Enrollments
            .CountAsync(e => e.CourseId == course.Id, cancellationToken);
        var enrolledStudentIds = await _db.Enrollments
            .Where(e => e.CourseId == course.Id)
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken);
        var enrolledSet = new HashSet<Guid>(enrolledStudentIds);

        // Preload candidate students by lowercased email so we make at most
        // one query for the whole batch. Filter to Role=Student here so non-
        // student matches get a clean "not a student" error instead of being
        // silently dropped.
        var distinctEmails = request.Rows
            .Select(r => (r.Email ?? string.Empty).Trim())
            .Where(e => e.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchingUsers = await _db.Users
            .AsNoTracking()
            .Where(u => distinctEmails.Contains(u.Email))
            .Select(u => new { u.Id, u.Email, u.Role, u.IsActive })
            .ToListAsync(cancellationToken);
        var userByEmail = matchingUsers
            .ToDictionary(u => u.Email, u => u, StringComparer.OrdinalIgnoreCase);

        var results = new List<BulkEnrollRowResultDto>(request.Rows.Count);
        var enrolledThisBatch = 0;

        foreach (var row in request.Rows)
        {
            var email = (row.Email ?? string.Empty).Trim();
            var rowResult = new BulkEnrollRowResultDto
            {
                Line = row.Line,
                Email = email,
            };

            if (email.Length == 0)
            {
                rowResult.Status = "Failed";
                rowResult.Error = "Email is required.";
                results.Add(rowResult);
                continue;
            }

            if (!userByEmail.TryGetValue(email, out var user))
            {
                rowResult.Status = "Failed";
                rowResult.Error = $"No user with email '{email}'.";
                results.Add(rowResult);
                continue;
            }

            if (!string.Equals(user.Role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                rowResult.Status = "Failed";
                rowResult.Error = $"User '{email}' is a {user.Role}, not a Student.";
                results.Add(rowResult);
                continue;
            }

            if (!user.IsActive)
            {
                rowResult.Status = "Failed";
                rowResult.Error = $"User '{email}' is suspended.";
                results.Add(rowResult);
                continue;
            }

            if (enrolledSet.Contains(user.Id))
            {
                rowResult.Status = "Skipped";
                rowResult.StudentId = user.Id;
                rowResult.Error = "Already enrolled.";
                results.Add(rowResult);
                continue;
            }

            // Capacity check happens against the live count (existing + this batch).
            if (course.MaxStudents is int max
                && currentEnrollmentCount + enrolledThisBatch >= max)
            {
                rowResult.Status = "Failed";
                rowResult.Error = $"Course is at capacity ({max}).";
                results.Add(rowResult);
                continue;
            }

            _db.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = user.Id,
                CourseId = course.Id,
                EnrolledAt = DateTime.UtcNow,
                ProgressPercentage = 0m,
                IsCompleted = false,
            });
            enrolledSet.Add(user.Id);
            enrolledThisBatch++;

            _db.AuditLogs.Add(AdminAudit.Entry(
                actorId,
                "course.bulk_enrolled",
                "Enrollment",
                user.Id, // entityId is the student for easy filtering later
                new { courseId = course.Id, studentEmail = email }));

            rowResult.Status = "Enrolled";
            rowResult.StudentId = user.Id;
            results.Add(rowResult);
        }

        if (enrolledThisBatch > 0)
        {
            course.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new BulkEnrollResultDto
        {
            CourseId = course.Id,
            CourseTitle = course.Title,
            TotalRows = request.Rows.Count,
            EnrolledCount = results.Count(r => r.Status == "Enrolled"),
            SkippedCount = results.Count(r => r.Status == "Skipped"),
            FailureCount = results.Count(r => r.Status == "Failed"),
            CourseEnrolledTotal = currentEnrollmentCount + enrolledThisBatch,
            CourseMaxStudents = course.MaxStudents,
            Rows = results,
        };
    }
}
