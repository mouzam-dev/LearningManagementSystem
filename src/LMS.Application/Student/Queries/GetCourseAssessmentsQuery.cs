using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

public record GetCourseAssessmentsQuery(Guid CourseId)
    : IRequest<IReadOnlyList<AssessmentListItemDto>>;

public class GetCourseAssessmentsQueryHandler
    : IRequestHandler<GetCourseAssessmentsQuery, IReadOnlyList<AssessmentListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCourseAssessmentsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<AssessmentListItemDto>> Handle(
        GetCourseAssessmentsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var enrolled = await _db.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.CourseId == request.CourseId && e.StudentId == userId, cancellationToken);
        if (!enrolled)
        {
            throw new UnauthorizedAccessException("You must enrol in this course before viewing its assessments.");
        }

        return await _db.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == request.CourseId)
            .OrderBy(a => a.DueDate ?? DateTime.MaxValue)
            .ThenBy(a => a.Title)
            .Select(a => new AssessmentListItemDto
            {
                AssessmentId = a.Id,
                CourseId = a.CourseId,
                Title = a.Title,
                Type = a.Type,
                DueDate = a.DueDate,
                TimeLimit = a.TimeLimit,
                PassingScore = a.PassingScore,
                MaxAttempts = a.MaxAttempts,
                QuestionCount = a.Questions.Count,
                AttemptCount = a.Submissions.Count(s => s.StudentId == userId),
                BestScore = a.Submissions
                    .Where(s => s.StudentId == userId && s.Score != null)
                    .Max(s => (int?)s.Score),
                IsPassed = a.Submissions
                    .Where(s => s.StudentId == userId && s.Score != null)
                    .Any(s => s.Score >= a.PassingScore),
            })
            .ToListAsync(cancellationToken);
    }
}
