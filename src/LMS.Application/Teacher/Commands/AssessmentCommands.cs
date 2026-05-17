using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Application.Teacher.Queries;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

internal static class AssessmentRules
{
    public static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Quiz", "Assignment",
    };

    public static bool IsValidType(string? t) =>
        !string.IsNullOrWhiteSpace(t) && AllowedTypes.Contains(t);
}

// ---------------- Create -----------------------------------------------------

public record CreateAssessmentCommand(
    Guid CourseId,
    string Title,
    string Type,
    int? TimeLimit,
    int PassingScore,
    DateTime? DueDate,
    int? MaxAttempts
) : IRequest<TeacherAssessmentDto>;

public class CreateAssessmentCommandValidator : AbstractValidator<CreateAssessmentCommand>
{
    public CreateAssessmentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).Must(AssessmentRules.IsValidType)
            .WithMessage("Type must be either Quiz or Assignment.");
        RuleFor(x => x.PassingScore).InclusiveBetween(0, 100)
            .WithMessage("Passing score must be between 0 and 100.");
        RuleFor(x => x.TimeLimit).Must(t => t is null || t > 0)
            .WithMessage("Time limit must be at least 1 minute.");
        RuleFor(x => x.MaxAttempts).Must(m => m is null || m > 0)
            .WithMessage("Max attempts must be at least 1.");
    }
}

public class CreateAssessmentCommandHandler : IRequestHandler<CreateAssessmentCommand, TeacherAssessmentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public CreateAssessmentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<TeacherAssessmentDto> Handle(CreateAssessmentCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId
                && (c.TeacherId == teacherId
                    || c.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = request.Title.Trim(),
            Type = request.Type.Trim(),
            TimeLimit = request.TimeLimit,
            PassingScore = request.PassingScore,
            DueDate = request.DueDate,
            MaxAttempts = request.MaxAttempts,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Assessments.Add(assessment);

        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetTeacherAssessmentQuery(assessment.Id), cancellationToken);
    }
}

// ---------------- Update -----------------------------------------------------

public record UpdateAssessmentCommand(
    Guid AssessmentId,
    string Title,
    int? TimeLimit,
    int PassingScore,
    DateTime? DueDate,
    int? MaxAttempts
) : IRequest<TeacherAssessmentDto>;

public class UpdateAssessmentCommandValidator : AbstractValidator<UpdateAssessmentCommand>
{
    public UpdateAssessmentCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PassingScore).InclusiveBetween(0, 100);
        RuleFor(x => x.TimeLimit).Must(t => t is null || t > 0);
        RuleFor(x => x.MaxAttempts).Must(m => m is null || m > 0);
    }
}

public class UpdateAssessmentCommandHandler : IRequestHandler<UpdateAssessmentCommand, TeacherAssessmentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public UpdateAssessmentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<TeacherAssessmentDto> Handle(UpdateAssessmentCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var assessment = await _db.Assessments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId
                && (a.Course.TeacherId == teacherId
                    || a.Course.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment {request.AssessmentId} was not found.");

        assessment.Title = request.Title.Trim();
        assessment.TimeLimit = request.TimeLimit;
        assessment.PassingScore = request.PassingScore;
        assessment.DueDate = request.DueDate;
        assessment.MaxAttempts = request.MaxAttempts;
        assessment.Course.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetTeacherAssessmentQuery(assessment.Id), cancellationToken);
    }
}

// ---------------- Delete -----------------------------------------------------

public record DeleteAssessmentCommand(Guid AssessmentId) : IRequest<Unit>;

public class DeleteAssessmentCommandHandler : IRequestHandler<DeleteAssessmentCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteAssessmentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteAssessmentCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var assessment = await _db.Assessments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId
                && (a.Course.TeacherId == teacherId
                    || a.Course.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment {request.AssessmentId} was not found.");

        _db.Assessments.Remove(assessment); // EF cascades to Questions + Submissions
        assessment.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
