using System.Text.Json;
using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Application.Teacher.Queries;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

// ---------------- Create -----------------------------------------------------

public record CreateBankQuestionCommand(
    string QuestionText,
    string Type,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    string? Tag
) : IRequest<BankQuestionDto>;

public class CreateBankQuestionCommandValidator : AbstractValidator<CreateBankQuestionCommand>
{
    public CreateBankQuestionCommandValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Type).Must(QuestionRules.IsValidType)
            .WithMessage("Type must be one of: MCQ, TrueFalse, ShortAnswer, Essay.");
        RuleFor(x => x.Points).GreaterThan(0).WithMessage("Points must be at least 1.");
        RuleFor(x => x.Options)
            .Must((cmd, opts) =>
                !QuestionRules.RequiresOptions(cmd.Type) || (opts is not null && opts.Count >= 2))
            .WithMessage("MCQ questions need at least 2 options.");
        RuleFor(x => x.CorrectAnswer)
            .Must((cmd, ans) =>
                !QuestionRules.IsAutoGradable(cmd.Type) || !string.IsNullOrWhiteSpace(ans))
            .WithMessage("MCQ and TrueFalse questions need a correct answer.");
        RuleFor(x => x.Tag).MaximumLength(80);
    }
}

public class CreateBankQuestionCommandHandler
    : IRequestHandler<CreateBankQuestionCommand, BankQuestionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateBankQuestionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BankQuestionDto> Handle(CreateBankQuestionCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();
        var now = DateTime.UtcNow;

        var bq = new BankQuestion
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            QuestionText = request.QuestionText.Trim(),
            Type = request.Type.Trim(),
            Options = request.Options is { Count: > 0 } ? JsonSerializer.Serialize(request.Options) : null,
            CorrectAnswer = string.IsNullOrWhiteSpace(request.CorrectAnswer) ? null : request.CorrectAnswer.Trim(),
            Points = request.Points,
            Tag = string.IsNullOrWhiteSpace(request.Tag) ? null : request.Tag.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.BankQuestions.Add(bq);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(bq);
    }

    internal static BankQuestionDto MapToDto(BankQuestion q) => new()
    {
        Id = q.Id,
        QuestionText = q.QuestionText,
        Type = q.Type,
        Options = GetBankQuestionQueryHandler.ParseOptions(q.Options),
        CorrectAnswer = q.CorrectAnswer,
        Points = q.Points,
        Tag = q.Tag,
        CreatedAt = q.CreatedAt,
        UpdatedAt = q.UpdatedAt,
    };
}

// ---------------- Update -----------------------------------------------------

public record UpdateBankQuestionCommand(
    Guid Id,
    string QuestionText,
    string Type,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    string? Tag
) : IRequest<BankQuestionDto>;

public class UpdateBankQuestionCommandValidator : AbstractValidator<UpdateBankQuestionCommand>
{
    public UpdateBankQuestionCommandValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Type).Must(QuestionRules.IsValidType);
        RuleFor(x => x.Points).GreaterThan(0);
        RuleFor(x => x.Options)
            .Must((cmd, opts) =>
                !QuestionRules.RequiresOptions(cmd.Type) || (opts is not null && opts.Count >= 2))
            .WithMessage("MCQ questions need at least 2 options.");
        RuleFor(x => x.CorrectAnswer)
            .Must((cmd, ans) =>
                !QuestionRules.IsAutoGradable(cmd.Type) || !string.IsNullOrWhiteSpace(ans))
            .WithMessage("MCQ and TrueFalse questions need a correct answer.");
        RuleFor(x => x.Tag).MaximumLength(80);
    }
}

public class UpdateBankQuestionCommandHandler
    : IRequestHandler<UpdateBankQuestionCommand, BankQuestionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateBankQuestionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BankQuestionDto> Handle(UpdateBankQuestionCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var bq = await _db.BankQuestions
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TeacherId == teacherId, cancellationToken)
            ?? throw new KeyNotFoundException($"Bank question {request.Id} was not found.");

        bq.QuestionText = request.QuestionText.Trim();
        bq.Type = request.Type.Trim();
        bq.Options = request.Options is { Count: > 0 } ? JsonSerializer.Serialize(request.Options) : null;
        bq.CorrectAnswer = string.IsNullOrWhiteSpace(request.CorrectAnswer) ? null : request.CorrectAnswer.Trim();
        bq.Points = request.Points;
        bq.Tag = string.IsNullOrWhiteSpace(request.Tag) ? null : request.Tag.Trim();
        bq.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return CreateBankQuestionCommandHandler.MapToDto(bq);
    }
}

// ---------------- Delete -----------------------------------------------------

public record DeleteBankQuestionCommand(Guid Id) : IRequest<Unit>;

public class DeleteBankQuestionCommandHandler : IRequestHandler<DeleteBankQuestionCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteBankQuestionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteBankQuestionCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var bq = await _db.BankQuestions
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TeacherId == teacherId, cancellationToken)
            ?? throw new KeyNotFoundException($"Bank question {request.Id} was not found.");

        _db.BankQuestions.Remove(bq);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// ---------------- Import into assessment ------------------------------------

/// <summary>
/// Copies the selected bank questions into the given assessment as new
/// <see cref="Question"/> rows. COPY semantics: a future edit to the bank
/// question does not change what's in the assessment. Returns the refreshed
/// assessment payload so the editor UI can render the appended questions
/// without an extra round-trip.
/// </summary>
public record ImportBankQuestionsToAssessmentCommand(
    Guid AssessmentId,
    IReadOnlyList<Guid> BankQuestionIds
) : IRequest<TeacherAssessmentDto>;

public class ImportBankQuestionsToAssessmentCommandValidator
    : AbstractValidator<ImportBankQuestionsToAssessmentCommand>
{
    public ImportBankQuestionsToAssessmentCommandValidator()
    {
        RuleFor(x => x.BankQuestionIds).NotNull()
            .Must(ids => ids.Count > 0).WithMessage("Pick at least one question to import.")
            .Must(ids => ids.Count <= 100).WithMessage("Import up to 100 questions at a time.");
    }
}

public class ImportBankQuestionsToAssessmentCommandHandler
    : IRequestHandler<ImportBankQuestionsToAssessmentCommand, TeacherAssessmentDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public ImportBankQuestionsToAssessmentCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<TeacherAssessmentDto> Handle(
        ImportBankQuestionsToAssessmentCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        // Assessment must be in a course the teacher owns or co-instructs.
        var assessment = await _db.Assessments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId
                && (a.Course.TeacherId == teacherId
                    || a.Course.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment {request.AssessmentId} was not found.");

        // Only pull bank questions the teacher actually owns. Anything in
        // BankQuestionIds the teacher doesn't own is silently dropped — this
        // is the same shape as the existing "private bank" rule on every
        // bank query and avoids leaking existence of other teachers' rows.
        var distinctIds = request.BankQuestionIds.Distinct().ToList();
        var bankQuestions = await _db.BankQuestions
            .Where(q => q.TeacherId == teacherId && distinctIds.Contains(q.Id))
            .ToListAsync(cancellationToken);

        if (bankQuestions.Count == 0)
        {
            throw new KeyNotFoundException("None of the selected bank questions were found.");
        }

        var nextOrder = (await _db.Questions
            .Where(q => q.AssessmentId == assessment.Id)
            .Select(q => (int?)q.Order)
            .MaxAsync(cancellationToken) ?? 0) + 1;

        // Preserve the order the teacher selected them in by keying off the
        // request order rather than the bank's CreatedAt.
        var ordered = distinctIds
            .Select(id => bankQuestions.FirstOrDefault(b => b.Id == id))
            .Where(b => b is not null)
            .Cast<BankQuestion>();

        foreach (var bq in ordered)
        {
            _db.Questions.Add(new Question
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessment.Id,
                QuestionText = bq.QuestionText,
                Type = bq.Type,
                Options = bq.Options,
                CorrectAnswer = bq.CorrectAnswer,
                Points = bq.Points,
                Order = nextOrder++,
            });
        }

        assessment.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetTeacherAssessmentQuery(assessment.Id), cancellationToken);
    }
}
