using FluentValidation;
using LMS.Application.Common;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Devices;

/// <summary>Store this phone's push token for the signed-in user (called by the Android app after sign-in).</summary>
public record RegisterDeviceCommand(string Token, string? Platform) : IRequest;

/// <summary>Forget this phone's push token (called on sign-out).</summary>
public record UnregisterDeviceCommand(string Token) : IRequest;

public class RegisterDeviceValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Platform).MaximumLength(20);
    }
}

public class UnregisterDeviceValidator : AbstractValidator<UnregisterDeviceCommand>
{
    public UnregisterDeviceValidator() => RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
}

public class DeviceCommandHandlers(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<RegisterDeviceCommand>, IRequestHandler<UnregisterDeviceCommand>
{
    public async Task Handle(RegisterDeviceCommand request, CancellationToken ct)
    {
        var userId = currentUser.GetUserId();
        var now = DateTime.UtcNow;
        var row = await db.DeviceTokens.FirstOrDefaultAsync(t => t.Token == request.Token, ct);
        if (row is null)
        {
            db.DeviceTokens.Add(new DeviceToken
            {
                Id = Guid.NewGuid(), UserId = userId, Token = request.Token,
                Platform = string.IsNullOrWhiteSpace(request.Platform) ? "android" : request.Platform.Trim().ToLowerInvariant(),
                CreatedAt = now, LastSeenAt = now,
            });
        }
        else
        {
            row.UserId = userId; // the phone now belongs to whoever signed in last
            row.LastSeenAt = now;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(UnregisterDeviceCommand request, CancellationToken ct)
    {
        var userId = currentUser.GetUserId();
        var row = await db.DeviceTokens.FirstOrDefaultAsync(t => t.Token == request.Token && t.UserId == userId, ct);
        if (row is null) return;
        db.DeviceTokens.Remove(row);
        await db.SaveChangesAsync(ct);
    }
}
