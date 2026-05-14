using FluentValidation;
using LMS.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Profile.Commands;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest<Unit>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Your current password is required.");

        // Mirrors the registration policy: min 8 chars, an upper, a lower, a digit.
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("A new password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("New password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("New password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("New password must contain a digit.");

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("The new password must be different from the current one.");
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Unit>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _hasher;

    public ChangePasswordCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IPasswordHasher hasher)
    {
        _db = db;
        _currentUser = currentUser;
        _hasher = hasher;
    }

    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Your account was not found.");

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            // Surfaced as 409 Conflict by the controller — a deliberate, friendly
            // "that didn't match" rather than a 400 validation error.
            throw new InvalidOperationException("Your current password is incorrect.");
        }

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
