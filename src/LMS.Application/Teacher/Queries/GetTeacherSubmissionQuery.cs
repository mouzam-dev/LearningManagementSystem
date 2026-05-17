using System.Text.Json;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Queries;

public record GetTeacherSubmissionQuery(Guid SubmissionId) : IRequest<TeacherSubmissionDetailDto>;

public class GetTeacherSubmissionQueryHandler
    : IRequestHandler<GetTeacherSubmissionQuery, TeacherSubmissionDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetTeacherSubmissionQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherSubmissionDetailDto> Handle(GetTeacherSubmissionQuery request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var submission = await _db.Submissions
            .AsNoTracking()
            .Include(s => s.Student)
            .Include(s => s.Assessment).ThenInclude(a => a.Course)
            .Include(s => s.Assessment).ThenInclude(a => a.Questions.OrderBy(q => q.Order))
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId
                && (s.Assessment.Course.TeacherId == teacherId
                    || s.Assessment.Course.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Submission {request.SubmissionId} was not found.");

        var totalPoints = submission.Assessment.Questions.Sum(q => q.Points);

        // Parse the answer map once.
        Dictionary<string, string> answerMap;
        try
        {
            answerMap = JsonSerializer.Deserialize<Dictionary<string, string>>(submission.Answers)
                        ?? new();
        }
        catch (JsonException)
        {
            answerMap = new();
        }

        // For Assignment: the single response lives under the synthetic key.
        var assignmentResponse = answerMap.TryGetValue("assignment-response", out var r) ? r : null;

        // For Quiz: build a per-question breakdown.
        var perQuestion = submission.Assessment.Questions
            .OrderBy(q => q.Order)
            .Select(q =>
            {
                answerMap.TryGetValue(q.Id.ToString(), out var studentAnswer);
                var autoGradable = q.Type == "MCQ" || q.Type == "TrueFalse";
                var isCorrect = autoGradable
                    && !string.IsNullOrWhiteSpace(q.CorrectAnswer)
                    && string.Equals(
                        (studentAnswer ?? string.Empty).Trim(),
                        q.CorrectAnswer.Trim(),
                        StringComparison.OrdinalIgnoreCase);

                return new SubmissionAnswerDto
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    Type = q.Type,
                    StudentAnswer = studentAnswer,
                    CorrectAnswer = autoGradable ? q.CorrectAnswer : null,
                    IsCorrect = isCorrect,
                    Points = q.Points,
                    Order = q.Order,
                };
            })
            .ToList();

        return new TeacherSubmissionDetailDto
        {
            SubmissionId = submission.Id,
            SubmittedAt = submission.SubmittedAt,
            GradedAt = submission.GradedAt,
            Score = submission.Score,
            Feedback = submission.Feedback,
            AssessmentId = submission.AssessmentId,
            AssessmentTitle = submission.Assessment.Title,
            AssessmentType = submission.Assessment.Type,
            PassingScore = submission.Assessment.PassingScore,
            TotalPoints = totalPoints,
            CourseId = submission.Assessment.CourseId,
            CourseTitle = submission.Assessment.Course.Title,
            StudentId = submission.StudentId,
            StudentName = (submission.Student.FirstName + " " + submission.Student.LastName).Trim(),
            StudentEmail = submission.Student.Email,
            Answers = perQuestion,
            AssignmentResponse = assignmentResponse,
        };
    }
}
