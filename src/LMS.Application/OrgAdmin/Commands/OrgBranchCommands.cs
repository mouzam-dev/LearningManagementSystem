using FluentValidation;
using LMS.Application.Admin;
using LMS.Application.Common;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.OrgAdmin.Commands;

// ---------------- Create branch in caller's org ----------------------------

public record OrgCreateBranchCommand(
    string Name, string? Code, string? Location, string? ContactEmail)
    : IRequest<BranchListItemDto>;

public class OrgCreateBranchCommandValidator : AbstractValidator<OrgCreateBranchCommand>
{
    public OrgCreateBranchCommandValidator()
    {
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

public class OrgCreateBranchCommandHandler
    : IRequestHandler<OrgCreateBranchCommand, BranchListItemDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OrgCreateBranchCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BranchListItemDto> Handle(OrgCreateBranchCommand request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();
        if (code is not null)
        {
            var taken = await _db.Branches
                .AnyAsync(b => b.OrganizationId == orgId && b.Code == code, ct);
            if (taken)
            {
                throw new InvalidOperationException(
                    $"Branch code '{code}' is already in use in this organization.");
            }
        }

        var now = DateTime.UtcNow;
        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
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
            new { branch.Name, branch.Code, branch.OrganizationId, source = "orgadmin" }));

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

// ---------------- Update branch (must belong to caller's org) --------------

public record OrgUpdateBranchCommand(
    Guid BranchId,
    string Name,
    string? Code,
    string? Location,
    string? ContactEmail,
    bool IsActive) : IRequest<BranchListItemDto>;

public class OrgUpdateBranchCommandValidator : AbstractValidator<OrgUpdateBranchCommand>
{
    public OrgUpdateBranchCommandValidator()
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

public class OrgUpdateBranchCommandHandler
    : IRequestHandler<OrgUpdateBranchCommand, BranchListItemDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OrgUpdateBranchCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BranchListItemDto> Handle(OrgUpdateBranchCommand request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var branch = await _db.Branches
            .FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new KeyNotFoundException($"Branch {request.BranchId} was not found.");

        if (branch.OrganizationId != orgId)
        {
            throw new InvalidOperationException("Branch belongs to a different organization.");
        }

        var code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim().ToUpperInvariant();
        if (code is not null)
        {
            var taken = await _db.Branches
                .AnyAsync(b => b.OrganizationId == orgId && b.Code == code && b.Id != branch.Id, ct);
            if (taken)
            {
                throw new InvalidOperationException(
                    $"Branch code '{code}' is already in use in this organization.");
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
            new { branch.Name, branch.Code, branch.IsActive, source = "orgadmin" }));

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
