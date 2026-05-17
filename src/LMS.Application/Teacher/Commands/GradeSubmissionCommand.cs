using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Application.Teacher.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

public record GradeSubmissionCommand(
    Guid SubmissionId,
    int Score,
    string? Feedback
) : IRequest<TeacherSubmissionDetailDto>;

public class GradeSubmissionCommandValidator : AbstractValidator<GradeSubmissionCommand>
{
    public GradeSubmissionCommandValidator()
    {
        RuleFor(x => x.Score).InclusiveBetween(0, 100)
            .WithMessage("Score must be between 0 and 100.");
        RuleFor(x => x.Feedback).MaximumLength(4000);
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

        submission.Score = request.Score;
        submission.Feedback = string.IsNullOrWhiteSpace(request.Feedback) ? null : request.Feedback.Trim();
        submission.GradedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetTeacherSubmissionQuery(submission.Id), cancellationToken);
    }
}
