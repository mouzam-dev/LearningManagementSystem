using System.Text.Json;
using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Queries;

public record GetAssessmentQuery(Guid AssessmentId) : IRequest<AssessmentDetailDto>;

public class GetAssessmentQueryHandler : IRequestHandler<GetAssessmentQuery, AssessmentDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAssessmentQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AssessmentDetailDto> Handle(GetAssessmentQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var assessment = await _db.Assessments
            .AsNoTracking()
            .Include(a => a.Course)
            .Include(a => a.Questions.OrderBy(q => q.Order))
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment {request.AssessmentId} was not found.");

        var enrolled = await _db.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.CourseId == assessment.CourseId && e.StudentId == userId, cancellationToken);
        if (!enrolled)
        {
            throw new UnauthorizedAccessException("You must enrol in the parent course to take this assessment.");
        }

        var submissions = await _db.Submissions
            .AsNoTracking()
            .Where(s => s.StudentId == userId && s.AssessmentId == assessment.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(cancellationToken);

        var totalPoints = assessment.Questions.Sum(q => q.Points);

        var lastSubmission = submissions.FirstOrDefault();
        SubmissionResultDto? lastDto = null;
        if (lastSubmission is not null)
        {
            lastDto = new SubmissionResultDto
            {
                SubmissionId = lastSubmission.Id,
                AssessmentId = assessment.Id,
                AssessmentType = assessment.Type,
                SubmittedAt = lastSubmission.SubmittedAt,
                GradedAt = lastSubmission.GradedAt,
                Score = lastSubmission.Score,
                TotalPoints = totalPoints,
                ScorePercentage = lastSubmission.Score is not null && totalPoints > 0
                    ? (int)Math.Round((double)lastSubmission.Score.Value / totalPoints * 100)
                    : null,
                IsPassed = lastSubmission.Score is int s && s >= assessment.PassingScore,
                Feedback = lastSubmission.Feedback,
            };
        }

        return new AssessmentDetailDto
        {
            AssessmentId = assessment.Id,
            CourseId = assessment.CourseId,
            CourseTitle = assessment.Course.Title,
            Title = assessment.Title,
            Type = assessment.Type,
            TimeLimit = assessment.TimeLimit,
            PassingScore = assessment.PassingScore,
            TotalPoints = totalPoints,
            DueDate = assessment.DueDate,
            MaxAttempts = assessment.MaxAttempts,
            AttemptCount = submissions.Count,
            AttemptsRemaining = assessment.MaxAttempts is int max ? Math.Max(0, max - submissions.Count) : null,
            Questions = assessment.Questions
                .OrderBy(q => q.Order)
                .Select(q => new QuestionDto
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    Type = q.Type,
                    Options = ParseOptions(q.Options),
                    Points = q.Points,
                    Order = q.Order,
                })
                .ToList(),
            LastSubmission = lastDto,
        };
    }

    private static IReadOnlyList<string>? ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var arr = JsonSerializer.Deserialize<List<string>>(json);
            return arr;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
