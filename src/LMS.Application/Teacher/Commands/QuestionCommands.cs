using System.Text.Json;
using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

internal static class QuestionRules
{
    public static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MCQ", "TrueFalse", "ShortAnswer", "Essay",
    };

    public static bool IsValidType(string? t) =>
        !string.IsNullOrWhiteSpace(t) && AllowedTypes.Contains(t);

    public static bool RequiresOptions(string type) =>
        string.Equals(type, "MCQ", StringComparison.OrdinalIgnoreCase);

    public static bool IsAutoGradable(string type) =>
        type == "MCQ" || type == "TrueFalse";
}

// ---------------- Create -----------------------------------------------------

public record CreateQuestionCommand(
    Guid AssessmentId,
    string QuestionText,
    string Type,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points
) : IRequest<TeacherQuestionDto>;

public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
{
    public CreateQuestionCommandValidator()
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
    }
}

public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, TeacherQuestionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateQuestionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherQuestionDto> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var assessment = await _db.Assessments
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment {request.AssessmentId} was not found.");
        if (assessment.Course.TeacherId != teacherId)
        {
            throw new KeyNotFoundException($"Assessment {request.AssessmentId} was not found.");
        }

        var maxOrder = await _db.Questions
            .Where(q => q.AssessmentId == assessment.Id)
            .Select(q => (int?)q.Order)
            .MaxAsync(cancellationToken) ?? 0;

        var question = new Question
        {
            Id = Guid.NewGuid(),
            AssessmentId = assessment.Id,
            QuestionText = request.QuestionText.Trim(),
            Type = request.Type.Trim(),
            Options = request.Options is { Count: > 0 } ? JsonSerializer.Serialize(request.Options) : null,
            CorrectAnswer = string.IsNullOrWhiteSpace(request.CorrectAnswer) ? null : request.CorrectAnswer.Trim(),
            Points = request.Points,
            Order = maxOrder + 1,
        };
        _db.Questions.Add(question);

        assessment.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(question);
    }

    internal static TeacherQuestionDto MapToDto(Question q) => new()
    {
        QuestionId = q.Id,
        QuestionText = q.QuestionText,
        Type = q.Type,
        Options = ParseOptions(q.Options),
        CorrectAnswer = q.CorrectAnswer,
        Points = q.Points,
        Order = q.Order,
    };

    internal static IReadOnlyList<string>? ParseOptions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch (JsonException) { return null; }
    }
}

// ---------------- Update -----------------------------------------------------

public record UpdateQuestionCommand(
    Guid QuestionId,
    string QuestionText,
    string Type,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    int Points,
    int? Order
) : IRequest<TeacherQuestionDto>;

public class UpdateQuestionCommandValidator : AbstractValidator<UpdateQuestionCommand>
{
    public UpdateQuestionCommandValidator()
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
        RuleFor(x => x.Order).Must(o => o is null || o >= 1);
    }
}

public class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand, TeacherQuestionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateQuestionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherQuestionDto> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var question = await _db.Questions
            .Include(q => q.Assessment).ThenInclude(a => a.Course)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Question {request.QuestionId} was not found.");
        if (question.Assessment.Course.TeacherId != teacherId)
        {
            throw new KeyNotFoundException($"Question {request.QuestionId} was not found.");
        }

        question.QuestionText = request.QuestionText.Trim();
        question.Type = request.Type.Trim();
        question.Options = request.Options is { Count: > 0 } ? JsonSerializer.Serialize(request.Options) : null;
        question.CorrectAnswer = string.IsNullOrWhiteSpace(request.CorrectAnswer) ? null : request.CorrectAnswer.Trim();
        question.Points = request.Points;
        if (request.Order is int newOrder && newOrder != question.Order)
        {
            question.Order = newOrder;
        }

        question.Assessment.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return CreateQuestionCommandHandler.MapToDto(question);
    }
}

// ---------------- Delete -----------------------------------------------------

public record DeleteQuestionCommand(Guid QuestionId) : IRequest<Unit>;

public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteQuestionCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var question = await _db.Questions
            .Include(q => q.Assessment).ThenInclude(a => a.Course)
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Question {request.QuestionId} was not found.");
        if (question.Assessment.Course.TeacherId != teacherId)
        {
            throw new KeyNotFoundException($"Question {request.QuestionId} was not found.");
        }

        _db.Questions.Remove(question);
        question.Assessment.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
