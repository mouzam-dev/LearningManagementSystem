using FluentValidation;
using LMS.Application.Admin;
using LMS.Application.Common;
using LMS.Application.OrgAdmin.Dtos;
using LMS.Domain.Constants;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.OrgAdmin.Commands;

/// <summary>
/// Create a new teacher account directly under the OrgAdmin's organization and
/// a branch within that organization. The OrgAdmin can only target branches
/// that belong to their own org — cross-org targeting is rejected.
/// </summary>
public record CreateOrgTeacherCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid BranchId) : IRequest<OrgTeacherDto>;

public class CreateOrgTeacherCommandValidator : AbstractValidator<CreateOrgTeacherCommand>
{
    public CreateOrgTeacherCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty);
    }
}

public class CreateOrgTeacherCommandHandler
    : IRequestHandler<CreateOrgTeacherCommand, OrgTeacherDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _hasher;

    public CreateOrgTeacherCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IPasswordHasher hasher)
    {
        _db = db;
        _currentUser = currentUser;
        _hasher = hasher;
    }

    public async Task<OrgTeacherDto> Handle(CreateOrgTeacherCommand request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var branch = await _db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new KeyNotFoundException($"Branch {request.BranchId} was not found.");

        if (branch.OrganizationId != orgId)
        {
            throw new InvalidOperationException("Branch belongs to a different organization.");
        }
        if (!branch.IsActive)
        {
            throw new InvalidOperationException("Branch is inactive.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var emailTaken = await _db.Users.AnyAsync(u => u.Email == email, ct);
        if (emailTaken)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = Roles.Teacher,
            PasswordHash = _hasher.Hash(request.Password),
            IsVerified = true,
            IsActive = true,
            OrganizationId = orgId,
            BranchId = branch.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Users.Add(user);

        _db.AuditLogs.Add(AdminAudit.Entry(
            _currentUser.GetUserId(),
            "teacher.created",
            "User",
            user.Id,
            new { user.Email, organizationId = orgId, branchId = branch.Id, source = "orgadmin" }));

        await _db.SaveChangesAsync(ct);

        return new OrgTeacherDto
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActive = user.IsActive,
            BranchId = branch.Id,
            BranchName = branch.Name,
            CourseCount = 0,
            PublishedCourseCount = 0,
            CreatedAt = user.CreatedAt,
        };
    }
}
