using FluentValidation;
using LMS.Application.Admin.Dtos;
using LMS.Application.Admin.Queries;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Admin.Commands;

internal static class AdminUserRules
{
    public static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Student", "Teacher", "Admin",
    };

    public static bool IsValidRole(string? r) =>
        !string.IsNullOrWhiteSpace(r) && AllowedRoles.Contains(r);
}

// ---------------- Suspend / reactivate -------------------------------------

public record SetUserActiveCommand(Guid UserId, bool IsActive)
    : IRequest<AdminUserDetailDto>;

public class SetUserActiveCommandHandler
    : IRequestHandler<SetUserActiveCommand, AdminUserDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public SetUserActiveCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<AdminUserDetailDto> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} was not found.");

        // Don't let an admin suspend themselves and lock the system out.
        if (request.IsActive == false && user.Id == _currentUser.GetUserId())
        {
            throw new InvalidOperationException("You can't suspend your own account.");
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetAdminUserDetailQuery(user.Id), cancellationToken);
    }
}

// ---------------- Change role ----------------------------------------------

public record ChangeUserRoleCommand(Guid UserId, string Role)
    : IRequest<AdminUserDetailDto>;

public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.Role).Must(AdminUserRules.IsValidRole)
            .WithMessage("Role must be one of: Student, Teacher, Admin.");
    }
}

public class ChangeUserRoleCommandHandler
    : IRequestHandler<ChangeUserRoleCommand, AdminUserDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public ChangeUserRoleCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<AdminUserDetailDto> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} was not found.");

        // Don't let an admin demote themselves and lock everyone else out.
        var newRole = request.Role.Trim();
        if (user.Id == _currentUser.GetUserId() && !string.Equals(user.Role, newRole, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("You can't change your own role.");
        }

        // Avoid demoting the last remaining admin.
        if (string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(newRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var otherAdmins = await _db.Users
                .CountAsync(u => u.Role == "Admin" && u.Id != user.Id && u.IsActive, cancellationToken);
            if (otherAdmins == 0)
            {
                throw new InvalidOperationException("Can't demote the last active admin.");
            }
        }

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetAdminUserDetailQuery(user.Id), cancellationToken);
    }
}
