using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetCourseStudentDetailQuery(Guid CourseId, Guid StudentId)
    : IRequest<CourseStudentDetailDto>;

public class GetCourseStudentDetailQueryHandler
    : IRequestHandler<GetCourseStudentDetailQuery, CourseStudentDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseStudentDetailQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<CourseStudentDetailDto> Handle(
        GetCourseStudentDetailQuery request,
        CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId
                && (c.TeacherId == teacherId
                    || c.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        var enrollment = await _db.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .FirstOrDefaultAsync(e => e.CourseId == course.Id && e.StudentId == request.StudentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Student {request.StudentId} is not enrolled in this course.");

        // All published lessons in this course, ordered. Left-join lesson progress
        // for this student so unstarted lessons show up too.
        var lessons = await _db.Lessons
            .AsNoTracking()
            .Where(l => l.Module.CourseId == course.Id && l.IsPublished)
            .OrderBy(l => l.Module.Order)
            .ThenBy(l => l.Order)
            .Select(l => new
            {
                Lesson = l,
                ModuleTitle = l.Module.Title,
                ModuleOrder = l.Module.Order,
                Progress = _db.LessonProgress
                    .Where(p => p.LessonId == l.Id && p.UserId == request.StudentId)
                    .Select(p => new { p.CompletedAt, p.WatchTimeSeconds })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var lessonDtos = lessons.Select(x => new StudentLessonProgressDto
        {
            LessonId = x.Lesson.Id,
            LessonTitle = x.Lesson.Title,
            ModuleTitle = x.ModuleTitle,
            ModuleOrder = x.ModuleOrder,
            LessonOrder = x.Lesson.Order,
            IsCompleted = x.Progress?.CompletedAt != null,
            WatchTimeSeconds = x.Progress?.WatchTimeSeconds ?? 0,
            CompletedAt = x.Progress?.CompletedAt,
        }).ToList();

        var submissions = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.StudentId == request.StudentId && s.Assessment.CourseId == course.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new StudentSubmissionDto
            {
                SubmissionId = s.Id,
                AssessmentId = s.AssessmentId,
                AssessmentTitle = s.Assessment.Title,
                AssessmentType = s.Assessment.Type,
                PassingScore = s.Assessment.PassingScore,
                SubmittedAt = s.SubmittedAt,
                GradedAt = s.GradedAt,
                Score = s.Score,
                IsPassed = s.Score != null && s.Score >= s.Assessment.PassingScore,
            })
            .ToListAsync(cancellationToken);

        var cert = await _db.Certificates
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == request.StudentId && c.CourseId == course.Id, cancellationToken);

        // Last-activity = max of last lesson activity + last submission.
        var lessonLast = lessonDtos.Where(l => l.CompletedAt != null).Select(l => l.CompletedAt).DefaultIfEmpty(null).Max();
        var subLast = submissions.Select(s => (DateTime?)s.SubmittedAt).DefaultIfEmpty(null).Max();
        var lastActivity = lessonLast is null ? subLast : (subLast is null ? lessonLast : (lessonLast > subLast ? lessonLast : subLast));

        return new CourseStudentDetailDto
        {
            StudentId = enrollment.StudentId,
            StudentName = (enrollment.Student.FirstName + " " + enrollment.Student.LastName).Trim(),
            StudentEmail = enrollment.Student.Email,
            EnrolledAt = enrollment.EnrolledAt,
            ProgressPercentage = enrollment.ProgressPercentage,
            IsCompleted = enrollment.IsCompleted,
            CompletedAt = enrollment.CompletedAt,
            LastActivityAt = lastActivity,
            CourseId = course.Id,
            CourseTitle = course.Title,
            CertificateId = cert?.Id,
            CertificateVerifyCode = cert?.VerifyCode,
            Lessons = lessonDtos,
            Submissions = submissions,
        };
    }
}
