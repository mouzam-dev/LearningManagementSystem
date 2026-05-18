using System.Text.Json;
using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Application.Teacher.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

/// <summary>
/// Grade a submission. Two modes:
///   - Single score:  CriterionScores is null -> Score is taken as-is.
///   - Rubric:        CriterionScores is a map of CriterionId -> awarded
///                    points. We sum those, clamp to [0, 100], and store the
///                    breakdown JSON on the submission so the grader UI can
///                    rehydrate the inputs on the next visit.
/// </summary>
public record GradeSubmissionCommand(
    Guid SubmissionId,
    int? Score,
    string? Feedback,
    IReadOnlyDictionary<Guid, int>? CriterionScores
) : IRequest<TeacherSubmissionDetailDto>;

public class GradeSubmissionCommandValidator : AbstractValidator<GradeSubmissionCommand>
{
    public GradeSubmissionCommandValidator()
    {
        // Score is required UNLESS criterion scores are provided (then we
        // compute it server-side).
        RuleFor(x => x.Score)
            .NotNull().When(x => x.CriterionScores is null)
            .WithMessage("Score is required when no rubric breakdown is provided.")
            .InclusiveBetween(0, 100).When(x => x.Score.HasValue)
            .WithMessage("Score must be between 0 and 100.");
        RuleFor(x => x.Feedback).MaximumLength(4000);
        RuleFor(x => x.CriterionScores)
            .Must(cs => cs is null || cs.Values.All(v => v >= 0 && v <= 100))
            .WithMessage("Each criterion score must be between 0 and 100.");
    }
}

public class GradeSubmissionCommandHandler
    : IRequestHandler<GradeSubmissionCommand, TeacherSubmissionDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public GradeSubmissionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<TeacherSubmissionDetailDto> Handle(GradeSubmissionCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var submission = await _db.Submissions
            .Include(s => s.Assessment).ThenInclude(a => a.Course)
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId
                && (s.Assessment.Course.TeacherId == teacherId
                    || s.Assessment.Course.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Submission {request.SubmissionId} was not found.");

        if (request.CriterionScores is { Count: > 0 } cs)
        {
            // Verify every id belongs to the assessment's rubric so we don't
            // accidentally accept garbage keys from the client.
            if (submission.Assessment.RubricId is null)
            {
                throw new InvalidOperationException(
                    "This assessment has no rubric; submit a single Score instead.");
            }
            var criterionIds = await _db.RubricCriteria
                .Where(c => c.RubricId == submission.Assessment.RubricId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            var validIds = new HashSet<Guid>(criterionIds);
            var filtered = cs.Where(kv => validIds.Contains(kv.Key))
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);

            if (filtered.Count == 0)
            {
                throw new InvalidOperationException(
                    "No valid criterion scores were provided for this rubric.");
            }

            var sum = filtered.Values.Sum();
            submission.Score = Math.Clamp(sum, 0, 100);
            submission.CriterionScoresJson = JsonSerializer.Serialize(filtered);
        }
        else
        {
            submission.Score = request.Score!.Value;
            // Single-score grading clears any prior rubric breakdown so the
            // teacher can revert from rubric grading to a flat score.
            submission.CriterionScoresJson = null;
        }

        submission.Feedback = string.IsNullOrWhiteSpace(request.Feedback) ? null : request.Feedback.Trim();
        submission.GradedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetTeacherSubmissionQuery(submission.Id), cancellationToken);
    }
}
