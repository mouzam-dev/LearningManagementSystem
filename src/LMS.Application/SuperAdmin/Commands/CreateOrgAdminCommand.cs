using FluentValidation;
using LMS.Application.Admin;
using LMS.Application.Common;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Domain.Constants;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.SuperAdmin.Commands;

/// <summary>
/// Creates an Organization Admin user attached to the given organization. The
/// generated password is returned in the response — SuperAdmin shares it with
/// the new admin out-of-band on first sign-in.
/// </summary>
public record CreateOrgAdminCommand(
    Guid OrganizationId,
    string Email,
    string FirstName,
    string LastName,
    string Password) : IRequest<CreateOrgAdminResult>;

public class CreateOrgAdminResult
{
    public OrgAdminListItemDto Admin { get; set; } = new();
}

public class CreateOrgAdminCommandValidator : AbstractValidator<CreateOrgAdminCommand>
{
    public CreateOrgAdminCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEqual(Guid.Empty);
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
    }
}

public class CreateOrgAdminCommandHandler
    : IRequestHandler<CreateOrgAdminCommand, CreateOrgAdminResult>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _hasher;

    public CreateOrgAdminCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IPasswordHasher hasher)
    {
        _db = db;
        _currentUser = currentUser;
        _hasher = hasher;
    }

    public async Task<CreateOrgAdminResult> Handle(CreateOrgAdminCommand request, CancellationToken ct)
    {
        var orgExists = await _db.Organizations.AnyAsync(o => o.Id == request.OrganizationId, ct);
        if (!orgExists)
        {
            throw new KeyNotFoundException($"Organization {request.OrganizationId} was not found.");
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
            Role = Roles.OrgAdmin,
            PasswordHash = _hasher.Hash(request.Password),
            IsVerified = true,
            IsActive = true,
            OrganizationId = request.OrganizationId,
            // OrgAdmin manages the entire organization, not a single branch — no BranchId.
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Users.Add(user);

        _db.AuditLogs.Add(AdminAudit.Entry(
            _currentUser.GetUserId(),
            "orgadmin.created",
            "User",
            user.Id,
            new { user.Email, organizationId = request.OrganizationId }));

        await _db.SaveChangesAsync(ct);

        return new CreateOrgAdminResult
        {
            Admin = new OrgAdminListItemDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
            },
        };
    }
}
