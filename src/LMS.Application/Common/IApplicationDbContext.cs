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
    DbSet<Organization> Organizations { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Course> Courses { get; }
    DbSet<CourseCoInstructor> CourseCoInstructors { get; }
    DbSet<Module> Modules { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<LessonProgress> LessonProgress { get; }
    DbSet<Assessment> Assessments { get; }
    DbSet<Question> Questions { get; }
    DbSet<BankQuestion> BankQuestions { get; }
    DbSet<Rubric> Rubrics { get; }
    DbSet<RubricCriterion> RubricCriteria { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<Certificate> Certificates { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<CourseRating> CourseRatings { get; }
    DbSet<TeacherRating> TeacherRatings { get; }
    DbSet<UserToken> UserTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
