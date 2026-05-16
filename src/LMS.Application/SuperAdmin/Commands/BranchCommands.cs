using FluentValidation;
using LMS.Application.Admin;
using LMS.Application.Common;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.SuperAdmin.Commands;

// ---------------- Create ---------------------------------------------------

public record CreateBranchCommand(
    Guid OrganizationId,
    string Name,
    string? Code,
    string? Location,
    string? ContactEmail) : IRequest<BranchListItemDto>;

public class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(40)
            .Matches("^[A-Z0-9](?:[A-Z0-9_-]*[A-Z0-9])?$")
            .When(x => !string.IsNullOrWhiteSpace(x.Code))
            .WithMessage("Code must be uppercase letters, digits, hyphens, or underscores.");
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.ContactEmail).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}

public class CreateBranchCommandHandler
    : IRequestHandler<CreateBranchCommand, BranchListItemDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateBranchCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BranchListItemDto> Handle(CreateBranchCommand request, CancellationToken ct)
    {
        var orgExists = await _db.Organizations.AnyAsync(o => o.Id == request.OrganizationId, ct);
        if (!orgExists)
        {
            throw new KeyNotFoundException($"Organization {request.OrganizationId} was not found.");
        }

        var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();
        if (code is not null)
        {
            var taken = await _db.Branches
                .AnyAsync(b => b.OrganizationId == request.OrganizationId && b.Code == code, ct);
            if (taken)
            {
                throw new InvalidOperationException($"Branch code '{code}' is already in use in this organization.");
            }
        }

        var now = DateTime.UtcNow;
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            Name = request.Name.Trim(),
            Code = code,
            Location = request.Location?.Trim(),
            ContactEmail = request.ContactEmail?.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Branches.Add(branch);

        _db.AuditLogs.Add(AdminAudit.Entry(
            _currentUser.GetUserId(),
            "branch.created",
            "Branch",
            branch.Id,
            new { branch.Name, branch.Code, branch.OrganizationId }));

        await _db.SaveChangesAsync(ct);

        return new BranchListItemDto
        {
            Id = branch.Id,
            OrganizationId = branch.OrganizationId,
            Name = branch.Name,
            Code = branch.Code,
            Location = branch.Location,
            ContactEmail = branch.ContactEmail,
            IsActive = branch.IsActive,
            TeacherCount = 0,
            StudentCount = 0,
            CreatedAt = branch.CreatedAt,
        };
    }
}

// ---------------- Update ---------------------------------------------------

public record UpdateBranchCommand(
    Guid BranchId,
    string Name,
    string? Code,
    string? Location,
    string? ContactEmail,
    bool IsActive) : IRequest<BranchListItemDto>;

public class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).MaximumLength(40)
            .Matches("^[A-Z0-9](?:[A-Z0-9_-]*[A-Z0-9])?$")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.ContactEmail).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}

public class UpdateBranchCommandHandler
    : IRequestHandler<UpdateBranchCommand, BranchListItemDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateBranchCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BranchListItemDto> Handle(UpdateBranchCommand request, CancellationToken ct)
    {
        var branch = await _db.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new KeyNotFoundException($"Branch {request.BranchId} was not found.");

        var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();
        if (code is not null)
        {
            var taken = await _db.Branches
                .AnyAsync(b => b.OrganizationId == branch.OrganizationId
                            && b.Code == code
                            && b.Id != branch.Id, ct);
            if (taken)
            {
                throw new InvalidOperationException($"Branch code '{code}' is already in use in this organization.");
            }
        }

        branch.Name = request.Name.Trim();
        branch.Code = code;
        branch.Location = request.Location?.Trim();
        branch.ContactEmail = request.ContactEmail?.Trim();
        branch.IsActive = request.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;

        _db.AuditLogs.Add(AdminAudit.Entry(
            _currentUser.GetUserId(),
            "branch.updated",
            "Branch",
            branch.Id,
            new { branch.Name, branch.Code, branch.IsActive }));

        await _db.SaveChangesAsync(ct);

        var teacherCount = await _db.Users
            .CountAsync(u => u.BranchId == branch.Id && u.Role == "Teacher", ct);
        var studentCount = await _db.Users
            .CountAsync(u => u.BranchId == branch.Id && u.Role == "Student", ct);

        return new BranchListItemDto
        {
            Id = branch.Id,
            OrganizationId = branch.OrganizationId,
            Name = branch.Name,
            Code = branch.Code,
            Location = branch.Location,
            ContactEmail = branch.ContactEmail,
            IsActive = branch.IsActive,
            TeacherCount = teacherCount,
            StudentCount = studentCount,
            CreatedAt = branch.CreatedAt,
        };
    }
}
