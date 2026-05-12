using System.Text.Json;
using LMS.Application.Common;
using LMS.Application.Student.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Student.Commands;

/// <param name="Answers">Dictionary keyed by QuestionId (string Guid) → student's answer.</param>
public record SubmitAssessmentCommand(
    Guid AssessmentId,
    IReadOnlyDictionary<string, string> Answers
) : IRequest<SubmissionResultDto>;

public class SubmitAssessmentCommandHandler
    : IRequestHandler<SubmitAssessmentCommand, SubmissionResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubmitAssessmentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<SubmissionResultDto> Handle(SubmitAssessmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var assessment = await _db.Assessments
            .Include(a => a.Questions)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment {request.AssessmentId} was not found.");

        var enrolled = await _db.Enrollments
            .AnyAsync(e => e.CourseId == assessment.CourseId && e.StudentId == userId, cancellationToken);
        if (!enrolled)
        {
            throw new UnauthorizedAccessException("You must enrol in the parent course to submit this assessment.");
        }

        var prior = await _db.Submissions
            .CountAsync(s => s.StudentId == userId && s.AssessmentId == assessment.Id, cancellationToken);

        if (assessment.MaxAttempts is int max && prior >= max)
        {
            throw new InvalidOperationException(
                $"You've already used all {max} attempt(s) for this assessment.");
        }

        var totalPoints = assessment.Questions.Sum(q => q.Points);

        // Auto-grade quizzes by comparing each answer to its correct answer.
        // Assignments are stored ungraded (Score = null) for the teacher to grade.
        int? score = null;
        IReadOnlyList<QuestionResultDto>? perQuestion = null;
        DateTime? gradedAt = null;

        if (assessment.Type == "Quiz" && assessment.Questions.Count > 0)
        {
            var earned = 0;
            var results = new List<QuestionResultDto>();
            foreach (var q in assessment.Questions.OrderBy(q => q.Order))
            {
                request.Answers.TryGetValue(q.Id.ToString(), out var studentAnswer);
                var isCorrect = !string.IsNullOrWhiteSpace(q.CorrectAnswer)
                                && IsAutoGradable(q.Type)
                                && string.Equals(
                                    (studentAnswer ?? string.Empty).Trim(),
                                    q.CorrectAnswer.Trim(),
                                    StringComparison.OrdinalIgnoreCase);
                var awarded = isCorrect ? q.Points : 0;
                earned += awarded;

                results.Add(new QuestionResultDto
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    StudentAnswer = studentAnswer,
                    CorrectAnswer = IsAutoGradable(q.Type) ? q.CorrectAnswer : null,
                    IsCorrect = isCorrect,
                    PointsAwarded = awarded,
                    PointsPossible = q.Points,
                });
            }

            score = totalPoints > 0
                ? (int)Math.Round((double)earned / totalPoints * 100)
                : 0;
            perQuestion = results;
            gradedAt = DateTime.UtcNow;
        }

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            StudentId = userId,
            AssessmentId = assessment.Id,
            Answers = JsonSerializer.Serialize(request.Answers),
            Score = score,
            SubmittedAt = DateTime.UtcNow,
            GradedAt = gradedAt,
        };
        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync(cancellationToken);

        return new SubmissionResultDto
        {
            SubmissionId = submission.Id,
            AssessmentId = assessment.Id,
            AssessmentType = assessment.Type,
            SubmittedAt = submission.SubmittedAt,
            GradedAt = submission.GradedAt,
            Score = score,
            TotalPoints = totalPoints,
            ScorePercentage = score,
            IsPassed = score is int s && s >= assessment.PassingScore,
            Feedback = submission.Feedback,
            QuestionResults = perQuestion,
        };
    }

    private static bool IsAutoGradable(string type) =>
        type == "MCQ" || type == "TrueFalse";
}
