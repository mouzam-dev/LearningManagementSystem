using LMS.Application.Common;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Attendance.Common;

/// <summary>
/// Shared guard: the calling teacher must be the course's primary teacher or a
/// co-instructor to create/mark/lock its attendance. Mirrors how the rest of the
/// teacher feature treats primary + co-instructors identically.
/// </summary>
internal static class AttendanceAuthorization
{
    public static async Task<Course> RequireTeacherCourseAsync(
        IApplicationDbContext db, Guid courseId, Guid teacherId, CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new KeyNotFoundException($"Course {courseId} was not found.");

        var allowed = course.TeacherId == teacherId
            || await db.CourseCoInstructors
                .AnyAsync(ci => ci.CourseId == courseId && ci.UserId == teacherId, ct);

        if (!allowed)
        {
            throw new UnauthorizedAccessException("You do not teach this course.");
        }

        return course;
    }
}
