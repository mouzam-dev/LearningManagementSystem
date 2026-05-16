using FluentValidation;
using LMS.Application.Admin;
using LMS.Application.Common;
using LMS.Application.SuperAdmin.Dtos;
using LMS.Application.SuperAdmin.Queries;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.SuperAdmin.Commands;

// ---------------- Create ---------------------------------------------------

public record CreateOrganizationCommand(
    string Name,
    string? Slug,
    string? Description,
    string? ContactEmail,
    string? LogoUrl) : IRequest<OrganizationDetailDto>;

public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(80)
            .Matches("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug must be lowercase letters, digits, or hyphens.");
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ContactEmail).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.LogoUrl).MaximumLength(500);
    }
}

public class CreateOrganizationCommandHandler
    : IRequestHandler<CreateOrganizationCommand, OrganizationDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public CreateOrganizationCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<OrganizationDetailDto> Handle(
        CreateOrganizationCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var slugTaken = await _db.Organizations
                .AnyAsync(o => o.Slug == request.Slug, ct);
            if (slugTaken)
            {
                throw new InvalidOperationException($"Slug '{request.Slug}' is already in use.");
            }
        }

        var now = DateTime.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim().ToLowerInvariant(),
            Description = request.Description?.Trim(),
            ContactEmail = request.ContactEmail?.Trim(),
            LogoUrl = request.LogoUrl?.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Organizations.Add(org);

        _db.AuditLogs.Add(AdminAudit.Entry(
            _currentUser.GetUserId(),
            "organization.created",
            "Organization",
            org.Id,
            new { org.Name, org.Slug }));

        await _db.SaveChangesAsync(ct);

        return await _mediator.Send(new GetOrganizationDetailQuery(org.Id), ct);
    }
}

// ---------------- Update ---------------------------------------------------

public record UpdateOrganizationCommand(
    Guid OrganizationId,
    string Name,
    string? Slug,
    string? Description,
    string? ContactEmail,
    string? LogoUrl,
    bool IsActive) : IRequest<OrganizationDetailDto>;

public class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MaximumLength(80)
            .Matches("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ContactEmail).EmailAddress().MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.LogoUrl).MaximumLength(500);
    }
}

public class UpdateOrganizationCommandHandler
    : IRequestHandler<UpdateOrganizationCommand, OrganizationDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public UpdateOrganizationCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<OrganizationDetailDto> Handle(
        UpdateOrganizationCommand request, CancellationToken ct)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == request.OrganizationId, ct)
            ?? throw new KeyNotFoundException($"Organization {request.OrganizationId} was not found.");

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var slug = request.Slug.Trim().ToLowerInvariant();
            var slugTaken = await _db.Organizations
                .AnyAsync(o => o.Slug == slug && o.Id != org.Id, ct);
            if (slugTaken)
            {
                throw new InvalidOperationException($"Slug '{request.Slug}' is already in use.");
            }
            org.Slug = slug;
        }
        else
        {
            org.Slug = null;
        }

        org.Name = request.Name.Trim();
        org.Description = request.Description?.Trim();
        org.ContactEmail = request.ContactEmail?.Trim();
        org.LogoUrl = request.LogoUrl?.Trim();
        org.IsActive = request.IsActive;
        org.UpdatedAt = DateTime.UtcNow;

        _db.AuditLogs.Add(AdminAudit.Entry(
            _currentUser.GetUserId(),
            "organization.updated",
            "Organization",
            org.Id,
            new { org.Name, org.IsActive }));

        await _db.SaveChangesAsync(ct);

        return await _mediator.Send(new GetOrganizationDetailQuery(org.Id), ct);
    }
}
