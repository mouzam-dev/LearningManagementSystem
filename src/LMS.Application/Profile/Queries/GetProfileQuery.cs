using LMS.Application.Common;
using LMS.Application.Profile.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Profile.Queries;

public record GetProfileQuery() : IRequest<ProfileDto>;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetProfileQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetUserId();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Your account was not found.");

        return new ProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            Bio = user.Bio,
            ProfilePictureUrl = user.ProfilePictureUrl,
            IsVerified = user.IsVerified,
            CreatedAt = user.CreatedAt,
        };
    }
}
