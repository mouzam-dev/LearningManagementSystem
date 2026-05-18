using System.Text.Json;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetTeacherAssessmentQuery(Guid AssessmentId) : IRequest<TeacherAssessmentDto>;

public class GetTeacherAssessmentQueryHandler
    : IRequestHandler<GetTeacherAssessmentQuery, TeacherAssessmentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTeacherAssessmentQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherAssessmentDto> Handle(GetTeacherAssessmentQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var assessment = await _db.Assessments
            .AsNoTracking()
            .Include(a => a.Course)
            .Include(a => a.Questions.OrderBy(q => q.Order))
            .Include(a => a.Rubric!).ThenInclude(r => r.Criteria.OrderBy(c => c.Order))
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId
                && (a.Course.TeacherId == teacherId
                    || a.Course.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment {request.AssessmentId} was not found.");

        var submissionCount = await _db.Submissions
            .AsNoTracking()
            .CountAsync(s => s.AssessmentId == assessment.Id, cancellationToken);

        return new TeacherAssessmentDto
        {
            AssessmentId = assessment.Id,
            CourseId = assessment.CourseId,
            CourseTitle = assessment.Course.Title,
            Title = assessment.Title,
            Type = assessment.Type,
            TimeLimit = assessment.TimeLimit,
            PassingScore = assessment.PassingScore,
            DueDate = assessment.DueDate,
            MaxAttempts = assessment.MaxAttempts,
            TotalPoints = assessment.Questions.Sum(q => q.Points),
            SubmissionCount = submissionCount,
            RubricId = assessment.RubricId,
            Rubric = assessment.Rubric is null ? null : GetRubricQueryHandler.MapToDto(assessment.Rubric),
            Questions = assessment.Questions
                .OrderBy(q => q.Order)
                .Select(q => new TeacherQuestionDto
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    Type = q.Type,
                    Options = ParseOptions(q.Options),
                    CorrectAnswer = q.CorrectAnswer,
                    Points = q.Points,
                    Order = q.Order,
                })
                .ToList(),
        };
    }

    private static IReadOnlyList<string>? ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { return null; }
    }
}
