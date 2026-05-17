using System.Linq.Expressions;
using LMS.Domain.Entities;

namespace LMS.Application.Common;

/// <summary>
/// Reusable predicates for "is the current user allowed to teach on this
/// course?" — used as a Where(…) filter throughout the Teacher handlers so a
/// missing co-instructor row makes the course look invisible (a clean 404),
/// not a 200 that leaks data.
///
/// The predicates are pre-compiled <see cref="Expression{TDelegate}"/> trees
/// so EF Core can translate them to SQL — they're meant to be invoked via
/// LINQ extension methods (<c>q.Where(CourseAuthorization.TaughtBy(id))</c>),
/// not against in-memory objects.
/// </summary>
public static class CourseAuthorization
{
    /// <summary>
    /// True when the user is either the primary teacher (<see cref="Course.TeacherId"/>)
    /// or listed as a co-instructor.
    /// </summary>
    public static Expression<Func<Course, bool>> TaughtBy(Guid userId) =>
        c => c.TeacherId == userId
          || c.CoInstructors.Any(ci => ci.UserId == userId);
}
