using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Common;

/// <summary>
/// Persistence abstraction exposed to MediatR handlers. Lets handlers live in the
/// Application layer without taking a hard dependency on the concrete DbContext
/// in Infrastructure. Add new DbSets here as features land.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Course> Courses { get; }
    DbSet<Module> Modules { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<LessonProgress> LessonProgress { get; }
    DbSet<Assessment> Assessments { get; }
    DbSet<Question> Questions { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<Certificate> Certificates { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
