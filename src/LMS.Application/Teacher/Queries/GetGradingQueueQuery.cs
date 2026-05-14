using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

/// <summary>
/// Submissions across all of the teacher's courses, oldest-first.
/// includeGraded=false (default) -> only Score IS NULL rows (assignment manual-grade queue).
/// includeGraded=true            -> everything (so the inbox can also surface "recently graded").
/// </summary>
public record GetGradingQueueQuery(bool IncludeGraded = false)
    : IRequest<IReadOnlyList<GradingQueueItemDto>>;

public class GetGradingQueueQueryHandler
    : IRequestHandler<GetGradingQueueQuery, IReadOnlyList<GradingQueueItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetGradingQueueQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<GradingQueueItemDto>> Handle(
        GetGradingQueueQuery request,
        CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var query = _db.Submissions
            .AsNoTracking()
            .Where(s => s.Assessment.Course.TeacherId == teacherId);

        if (!request.IncludeGraded)
        {
            query = query.Where(s => s.Score == null);
        }

        return await query
            .OrderBy(s => s.Score == null ? 0 : 1)   // ungraded first
            .ThenBy(s => s.SubmittedAt)               // then oldest
            .Take(100)
            .Select(s => new GradingQueueItemDto
            {
                SubmissionId = s.Id,
                AssessmentId = s.AssessmentId,
                AssessmentTitle = s.Assessment.Title,
                AssessmentType = s.Assessment.Type,
                CourseId = s.Assessment.CourseId,
                CourseTitle = s.Assessment.Course.Title,
                StudentId = s.StudentId,
                StudentName = s.Student.FirstName + " " + s.Student.LastName,
                StudentEmail = s.Student.Email,
                SubmittedAt = s.SubmittedAt,
                Score = s.Score,
                GradedAt = s.GradedAt,
                TotalPoints = s.Assessment.Questions.Sum(q => q.Points),
                PassingScore = s.Assessment.PassingScore,
            })
            .ToListAsync(cancellationToken);
    }
}
