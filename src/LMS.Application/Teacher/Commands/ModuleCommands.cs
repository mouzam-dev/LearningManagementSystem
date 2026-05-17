using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

// ---------------- Create -----------------------------------------------------

public record CreateModuleCommand(
    Guid CourseId,
    string Title,
    string? Description
) : IRequest<TeacherModuleDto>;

public class CreateModuleCommandValidator : AbstractValidator<CreateModuleCommand>
{
    public CreateModuleCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class CreateModuleCommandHandler : IRequestHandler<CreateModuleCommand, TeacherModuleDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateModuleCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherModuleDto> Handle(CreateModuleCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        // Visible to the primary teacher OR any co-instructor. The inline
        // filter on CoInstructors translates to an EXISTS in SQL so an
        // unauthorized lookup returns null — and we surface a clean 404
        // rather than leak existence.
        var course = await _db.Courses
            .FirstOrDefaultAsync(c => c.Id == request.CourseId
                && (c.TeacherId == teacherId
                    || c.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Course {request.CourseId} was not found.");

        // Append to the end of the existing modules.
        var maxOrder = await _db.Modules
            .Where(m => m.CourseId == course.Id)
            .Select(m => (int?)m.Order)
            .MaxAsync(cancellationToken) ?? 0;

        var module = new Module
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Order = maxOrder + 1,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Modules.Add(module);

        course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new TeacherModuleDto
        {
            ModuleId = module.Id,
            Title = module.Title,
            Description = module.Description,
            Order = module.Order,
            Lessons = Array.Empty<TeacherLessonDto>(),
        };
    }
}

// ---------------- Update -----------------------------------------------------

public record UpdateModuleCommand(
    Guid ModuleId,
    string Title,
    string? Description,
    int? Order
) : IRequest<TeacherModuleDto>;

public class UpdateModuleCommandValidator : AbstractValidator<UpdateModuleCommand>
{
    public UpdateModuleCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Order).Must(o => o is null || o >= 1)
            .WithMessage("Order must be at least 1.");
    }
}

public class UpdateModuleCommandHandler : IRequestHandler<UpdateModuleCommand, TeacherModuleDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateModuleCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<TeacherModuleDto> Handle(UpdateModuleCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var module = await _db.Modules
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == request.ModuleId
                && (m.Course.TeacherId == teacherId
                    || m.Course.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Module {request.ModuleId} was not found.");

        module.Title = request.Title.Trim();
        module.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.Order is int newOrder && newOrder != module.Order)
        {
            module.Order = newOrder;
        }

        module.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new TeacherModuleDto
        {
            ModuleId = module.Id,
            Title = module.Title,
            Description = module.Description,
            Order = module.Order,
        };
    }
}

// ---------------- Delete -----------------------------------------------------

public record DeleteModuleCommand(Guid ModuleId) : IRequest<Unit>;

public class DeleteModuleCommandHandler : IRequestHandler<DeleteModuleCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteModuleCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteModuleCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var module = await _db.Modules
            .Include(m => m.Course)
            .FirstOrDefaultAsync(m => m.Id == request.ModuleId
                && (m.Course.TeacherId == teacherId
                    || m.Course.CoInstructors.Any(ci => ci.UserId == teacherId)), cancellationToken)
            ?? throw new KeyNotFoundException($"Module {request.ModuleId} was not found.");

        _db.Modules.Remove(module);   // EF cascade also removes lessons
        module.Course.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
