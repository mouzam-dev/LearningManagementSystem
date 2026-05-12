using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

// ---------------- Create -----------------------------------------------------

public record CreateLessonCommand(
    Guid ModuleId,
    string Title,
    string Type,
    string? Content,
    int? Duration,
    bool IsPublished
) : IRequest<TeacherLessonDto>;

public class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).NotEmpty().Must(LessonRules.IsValidType)
            .WithMessage("Type must be one of: Video, Document, Text, Quiz, Assignment.");
        RuleFor(x => x.Duration).Must(d => d is null || d > 0)
            .WithMessage("Duration must be at least 1 minute.");
    }
}

public class CreateLessonCommandHandler : IRequestHandler<CreateLessonCommand, TeacherLessonDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateLessonCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherLessonDto> Handle(CreateLessonCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var module = await _db.Modules
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Module {request.ModuleId} was not found.");
        if (module.Course.TeacherId != teacherId)
        {
            throw new KeyNotFoundException($"Module {request.ModuleId} was not found.");
        }

        var maxOrder = await _db.Lessons
            .Where(l => l.ModuleId == module.Id)
            .Select(l => (int?)l.Order)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTime.UtcNow;
        var lesson = new Lesson
        {
            Id = Guid.NewGuid(),
            ModuleId = module.Id,
            Title = request.Title.Trim(),
            Type = request.Type.Trim(),
            Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content,
            Duration = request.Duration,
            Order = maxOrder + 1,
            IsPublished = request.IsPublished,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Lessons.Add(lesson);

        module.Course.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(lesson);
    }

    internal static TeacherLessonDto MapToDto(Lesson l) => new()
    {
        LessonId = l.Id,
        Title = l.Title,
        Type = l.Type,
        Content = l.Content,
        Duration = l.Duration,
        Order = l.Order,
        IsPublished = l.IsPublished,
    };
}

// ---------------- Update -----------------------------------------------------

public record UpdateLessonCommand(
    Guid LessonId,
    string Title,
    string Type,
    string? Content,
    int? Duration,
    int? Order,
    bool IsPublished
) : IRequest<TeacherLessonDto>;

public class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).NotEmpty().Must(LessonRules.IsValidType)
            .WithMessage("Type must be one of: Video, Document, Text, Quiz, Assignment.");
        RuleFor(x => x.Duration).Must(d => d is null || d > 0)
            .WithMessage("Duration must be at least 1 minute.");
        RuleFor(x => x.Order).Must(o => o is null || o >= 1)
            .WithMessage("Order must be at least 1.");
    }
}

public class UpdateLessonCommandHandler : IRequestHandler<UpdateLessonCommand, TeacherLessonDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateLessonCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherLessonDto> Handle(UpdateLessonCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var lesson = await _db.Lessons
            .Include(l => l.Module).ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Lesson {request.LessonId} was not found.");
        if (lesson.Module.Course.TeacherId != teacherId)
        {
            throw new KeyNotFoundException($"Lesson {request.LessonId} was not found.");
        }

        lesson.Title = request.Title.Trim();
        lesson.Type = request.Type.Trim();
        lesson.Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content;
        lesson.Duration = request.Duration;
        lesson.IsPublished = request.IsPublished;
        if (request.Order is int newOrder && newOrder != lesson.Order)
        {
            lesson.Order = newOrder;
        }
        lesson.UpdatedAt = DateTime.UtcNow;
        lesson.Module.Course.UpdatedAt = lesson.UpdatedAt;

        await _db.SaveChangesAsync(cancellationToken);

        return CreateLessonCommandHandler.MapToDto(lesson);
    }
}

// ---------------- Delete -----------------------------------------------------

public record DeleteLessonCommand(Guid LessonId) : IRequest<Unit>;

public class DeleteLessonCommandHandler : IRequestHandler<DeleteLessonCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteLessonCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var lesson = await _db.Lessons
            .Include(l => l.Module).ThenInclude(m => m.Course)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Lesson {request.LessonId} was not found.");
        if (lesson.Module.Course.TeacherId != teacherId)
        {
            throw new KeyNotFoundException($"Lesson {request.LessonId} was not found.");
        }

        _db.Lessons.Remove(lesson);
        lesson.Module.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

internal static class LessonRules
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Video", "Document", "Text", "Quiz", "Assignment",
    };

    public static bool IsValidType(string? type) =>
        !string.IsNullOrWhiteSpace(type) && AllowedTypes.Contains(type);
}
