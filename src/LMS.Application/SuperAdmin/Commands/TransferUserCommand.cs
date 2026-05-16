using FluentValidation;
using LMS.Application.Admin;
using LMS.Application.Admin.Dtos;
using LMS.Application.Admin.Queries;
using LMS.Application.Common;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.SuperAdmin.Commands;

/// <summary>
/// Move a Teacher or Student to a different organization+branch — platform-wide
/// transfer authority reserved for SuperAdmin. The branch must belong to the
/// target organization. Role stays unchanged; only tenancy changes.
/// </summary>
public record TransferUserCommand(Guid UserId, Guid OrganizationId, Guid BranchId)
    : IRequest<AdminUserDetailDto>;

public class TransferUserCommandValidator : AbstractValidator<TransferUserCommand>
{
    public TransferUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
        RuleFor(x => x.OrganizationId).NotEqual(Guid.Empty);
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty);
    }
}

public class TransferUserCommandHandler
    : IRequestHandler<TransferUserCommand, AdminUserDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public TransferUserCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IMediator mediator)
    {
        _db = db;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<AdminUserDetailDto> Handle(TransferUserCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException($"User {request.UserId} was not found.");

        // Only Teachers and Students live in a branch. SuperAdmin has no tenancy
        // and OrgAdmin sits at the org (not branch) level — they're transferred
        // via the role-change flow instead.
        if (user.Role != Roles.Teacher && user.Role != Roles.Student)
        {
            throw new InvalidOperationException(
                $"Cannot transfer a {user.Role}. This action is for Teachers and Students only.");
        }

        var branch = await _db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new KeyNotFoundException($"Branch {request.BranchId} was not found.");

        if (branch.OrganizationId != request.OrganizationId)
        {
            throw new InvalidOperationException(
                "Branch does not belong to the target organization.");
        }

        var previousOrg = user.OrganizationId;
        var previousBranch = user.BranchId;

        if (previousOrg != request.OrganizationId || previousBranch != branch.Id)
        {
            user.OrganizationId = request.OrganizationId;
            user.BranchId = branch.Id;
            user.UpdatedAt = DateTime.UtcNow;

            // Carry the teacher's courses with them. Course.OrganizationId /
            // BranchId is denormalized from the owning teacher, so a transfer
            // that left courses behind would orphan them in the source org's
            // moderation list while the teacher (and their authored content)
            // moved on. Re-stamp before audit so a single SaveChanges commits
            // both the user row and its courses atomically.
            var movedCourseCount = 0;
            if (user.Role == Roles.Teacher)
            {
                var courses = await _db.Courses
                    .Where(c => c.TeacherId == user.Id)
                    .ToListAsync(ct);

                foreach (var course in courses)
                {
                    course.OrganizationId = request.OrganizationId;
                    course.BranchId = branch.Id;
                    course.UpdatedAt = user.UpdatedAt;
                }
                movedCourseCount = courses.Count;
            }

            _db.AuditLogs.Add(AdminAudit.Entry(
                _currentUser.GetUserId(),
                "user.transferred",
                "User",
                user.Id,
                new
                {
                    user.Email,
                    user.Role,
                    fromOrganizationId = previousOrg,
                    fromBranchId = previousBranch,
                    toOrganizationId = request.OrganizationId,
                    toBranchId = branch.Id,
                    movedCourseCount,
                }));

            await _db.SaveChangesAsync(ct);
        }

        return await _mediator.Send(new GetAdminUserDetailQuery(user.Id), ct);
    }
}
