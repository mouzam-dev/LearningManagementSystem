using FluentValidation;
using LMS.Application.Common;
using LMS.Application.Profile.Dtos;
using LMS.Application.Profile.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Profile.Commands;

public record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string? Bio,
    string? ProfilePictureUrl
) : IRequest<ProfileDto>;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);
        RuleFor(x => x.Bio)
            .MaximumLength(1000).WithMessage("Bio can be at most 1000 characters.");
        RuleFor(x => x.ProfilePictureUrl)
            .MaximumLength(500)
            .Must(BeAbsoluteUrlOrEmpty)
                .WithMessage("Profile picture URL must be a valid http(s) URL when provided.");
    }

    private static bool BeAbsoluteUrlOrEmpty(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var u)
            && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public UpdateProfileCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Your account was not found.");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        user.ProfilePictureUrl = string.IsNullOrWhiteSpace(request.ProfilePictureUrl)
            ? null
            : request.ProfilePictureUrl.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetProfileQuery(), cancellationToken);
    }
}
