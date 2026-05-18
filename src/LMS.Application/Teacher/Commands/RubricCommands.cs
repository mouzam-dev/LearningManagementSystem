using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Teacher.Dtos;
using LMS.Application.Teacher.Queries;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Teacher.Commands;

// ---------------- Create -----------------------------------------------------

public record CreateRubricCommand(
    string Name,
    string? Description,
    IReadOnlyList<RubricCriterionInput> Criteria
) : IRequest<RubricDto>;

public class CreateRubricCommandValidator : AbstractValidator<CreateRubricCommand>
{
    public CreateRubricCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Criteria).NotNull()
            .Must(c => c.Count >= 1).WithMessage("A rubric needs at least 1 criterion.")
            .Must(c => c.Count <= 20).WithMessage("Rubrics can have at most 20 criteria.");
        RuleForEach(x => x.Criteria).ChildRules(c =>
        {
            c.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            c.RuleFor(x => x.Description).MaximumLength(2000);
            c.RuleFor(x => x.MaxPoints).InclusiveBetween(1, 100);
        });
    }
}

public class CreateRubricCommandHandler : IRequestHandler<CreateRubricCommand, RubricDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateRubricCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RubricDto> Handle(CreateRubricCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();
        var now = DateTime.UtcNow;

        var rubric = new Rubric
        {
            Id = Guid.NewGuid(),
            TeacherId = teacherId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        var order = 1;
        foreach (var c in request.Criteria)
        {
            rubric.Criteria.Add(new RubricCriterion
            {
                Id = Guid.NewGuid(),
                RubricId = rubric.Id,
                Name = c.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(c.Description) ? null : c.Description.Trim(),
                MaxPoints = c.MaxPoints,
                Order = order++,
            });
        }
        _db.Rubrics.Add(rubric);
        await _db.SaveChangesAsync(cancellationToken);

        return GetRubricQueryHandler.MapToDto(rubric);
    }
}

// ---------------- Update (full replace of criteria) -------------------------

public record UpdateRubricCommand(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<RubricCriterionInput> Criteria
) : IRequest<RubricDto>;

public class UpdateRubricCommandValidator : AbstractValidator<UpdateRubricCommand>
{
    public UpdateRubricCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Criteria).NotNull()
            .Must(c => c.Count >= 1).WithMessage("A rubric needs at least 1 criterion.")
            .Must(c => c.Count <= 20).WithMessage("Rubrics can have at most 20 criteria.");
        RuleForEach(x => x.Criteria).ChildRules(c =>
        {
            c.RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            c.RuleFor(x => x.Description).MaximumLength(2000);
            c.RuleFor(x => x.MaxPoints).InclusiveBetween(1, 100);
        });
    }
}

public class UpdateRubricCommandHandler : IRequestHandler<UpdateRubricCommand, RubricDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateRubricCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<RubricDto> Handle(UpdateRubricCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var rubric = await _db.Rubrics
            .Include(r => r.Criteria)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TeacherId == teacherId, cancellationToken)
            ?? throw new KeyNotFoundException($"Rubric {request.Id} was not found.");

        rubric.Name = request.Name.Trim();
        rubric.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        rubric.UpdatedAt = DateTime.UtcNow;

        // Wipe-and-rebuild criteria. Simpler than diffing and the table is
        // small; cascade delete is configured on the FK. Past submissions
        // already store their per-criterion scores by id in CriterionScoresJson,
        // so the new criterion ids won't match — graders see the old scores
        // labelled "(unknown criterion)" until they re-grade. Acceptable for
        // MVP since editing a rubric after grading should be rare.
        _db.RubricCriteria.RemoveRange(rubric.Criteria);
        rubric.Criteria.Clear();

        var order = 1;
        foreach (var c in request.Criteria)
        {
            rubric.Criteria.Add(new RubricCriterion
            {
                Id = Guid.NewGuid(),
                RubricId = rubric.Id,
                Name = c.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(c.Description) ? null : c.Description.Trim(),
                MaxPoints = c.MaxPoints,
                Order = order++,
            });
        }
        await _db.SaveChangesAsync(cancellationToken);

        return GetRubricQueryHandler.MapToDto(rubric);
    }
}

// ---------------- Delete -----------------------------------------------------

public record DeleteRubricCommand(Guid Id) : IRequest<Unit>;

public class DeleteRubricCommandHandler : IRequestHandler<DeleteRubricCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteRubricCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(DeleteRubricCommand request, CancellationToken cancellationToken)
    {
        var teacherId = _currentUser.GetUserId();

        var rubric = await _db.Rubrics
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TeacherId == teacherId, cancellationToken)
            ?? throw new KeyNotFoundException($"Rubric {request.Id} was not found.");

        // Block deletion if any of this teacher's assessments still reference it.
        // (DB-level FK is Restrict, but checking first lets us return a clean
        // 409 with a message instead of relying on a SqlException.)
        var inUse = await _db.Assessments
            .AnyAsync(a => a.RubricId == request.Id, cancellationToken);
        if (inUse)
        {
            throw new InvalidOperationException(
                "This rubric is attached to one or more assessments. Detach it first, then delete.");
        }

        _db.Rubrics.Remove(rubric);
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
