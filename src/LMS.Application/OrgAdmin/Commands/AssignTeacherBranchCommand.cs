using FluentValidation;
using LMS.Application.Admin;
using LMS.Application.Common;
using LMS.Application.OrgAdmin.Dtos;
using LMS.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.OrgAdmin.Commands;

/// <summary>
/// Move a teacher to a different branch within the caller's organization.
/// Used by both the OrgAdmin teacher screen and the SuperAdmin "Map teacher
/// to org+branch" flow (SuperAdmin reaches this through the same handler;
/// see ReassignTeacherCommand below for the cross-org variant).
/// </summary>
public record AssignTeacherBranchCommand(Guid TeacherId, Guid BranchId)
    : IRequest<OrgTeacherDto>;

public class AssignTeacherBranchCommandValidator
    : AbstractValidator<AssignTeacherBranchCommand>
{
    public AssignTeacherBranchCommandValidator()
    {
        RuleFor(x => x.TeacherId).NotEqual(Guid.Empty);
        RuleFor(x => x.BranchId).NotEqual(Guid.Empty);
    }
}

public class AssignTeacherBranchCommandHandler
    : IRequestHandler<AssignTeacherBranchCommand, OrgTeacherDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AssignTeacherBranchCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OrgTeacherDto> Handle(AssignTeacherBranchCommand request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var teacher = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.TeacherId, ct)
            ?? throw new KeyNotFoundException($"Teacher {request.TeacherId} was not found.");

        if (teacher.Role != Roles.Teacher)
        {
            throw new InvalidOperationException("User is not a teacher.");
        }
        if (teacher.OrganizationId != orgId)
        {
            throw new InvalidOperationException("Teacher belongs to a different organization.");
        }

        var branch = await _db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new KeyNotFoundException($"Branch {request.BranchId} was not found.");

        if (branch.OrganizationId != orgId)
        {
            throw new InvalidOperationException("Target branch belongs to a different organization.");
        }

        if (teacher.BranchId != branch.Id)
        {
            teacher.BranchId = branch.Id;
            teacher.UpdatedAt = DateTime.UtcNow;

            _db.AuditLogs.Add(AdminAudit.Entry(
                _currentUser.GetUserId(),
                "teacher.branch_assigned",
                "User",
                teacher.Id,
                new { teacher.Email, branchId = branch.Id, branchName = branch.Name }));

            await _db.SaveChangesAsync(ct);
        }

        var courseCount = await _db.Courses.CountAsync(c => c.TeacherId == teacher.Id, ct);
        var publishedCount = await _db.Courses
            .CountAsync(c => c.TeacherId == teacher.Id && c.IsPublished, ct);

        return new OrgTeacherDto
        {
            UserId = teacher.Id,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            Email = teacher.Email,
            IsActive = teacher.IsActive,
            BranchId = teacher.BranchId,
            BranchName = branch.Name,
            CourseCount = courseCount,
            PublishedCourseCount = publishedCount,
            CreatedAt = teacher.CreatedAt,
        };
    }
}
