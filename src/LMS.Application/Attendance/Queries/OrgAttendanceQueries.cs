using LMS.Application.Attendance.Common;
using LMS.Application.Attendance.Dtos;
using LMS.Application.Common;
using LMS.Application.OrgAdmin;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Attendance.Queries;

// --------------------- Branch-wise rollup across the whole org ---------------------
//
// NOTE: aggregates in memory after pulling (branch, course, student, status) tuples
// for the org+date-range. Fine at this app's scale; if data grows large, move the
// rollup into a SQL GROUP BY with CASE weights or a summary table.

public record GetOrgAttendanceOverviewQuery(DateOnly? FromDate, DateOnly? ToDate)
    : IRequest<OrgAttendanceOverviewDto>;

public class GetOrgAttendanceOverviewQueryHandler
    : IRequestHandler<GetOrgAttendanceOverviewQuery, OrgAttendanceOverviewDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOrgAttendanceOverviewQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OrgAttendanceOverviewDto> Handle(GetOrgAttendanceOverviewQuery request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var q = _db.AttendanceRecords.AsNoTracking()
            .Where(r => r.Session.OrganizationId == orgId && r.Session.Status != AttendanceSessionStatus.Cancelled);
        if (request.FromDate is not null) q = q.Where(r => r.SessionDate >= request.FromDate.Value);
        if (request.ToDate is not null) q = q.Where(r => r.SessionDate <= request.ToDate.Value);

        var rows = await q
            .Select(r => new { r.BranchId, r.CourseId, r.StudentId, r.AttendanceSessionId, r.Status })
            .ToListAsync(ct);

        var nameById = await _db.Branches.AsNoTracking()
            .Where(b => b.OrganizationId == orgId)
            .ToDictionaryAsync(b => b.Id, b => b.Name, ct);

        var branches = rows
            .GroupBy(r => r.BranchId)
            .Select(g =>
            {
                var statuses = g.Select(x => x.Status).ToList();
                return new BranchAttendanceRowDto
                {
                    BranchId = g.Key,
                    BranchName = g.Key is Guid bid && nameById.TryGetValue(bid, out var n) ? n : "(no branch)",
                    CourseCount = g.Select(x => x.CourseId).Distinct().Count(),
                    SessionCount = g.Select(x => x.AttendanceSessionId).Distinct().Count(),
                    StudentCount = g.Select(x => x.StudentId).Distinct().Count(),
                    RecordCount = statuses.Count,
                    AttendancePercent = AttendanceWeights.Percent(statuses),
                };
            })
            .OrderBy(b => b.BranchName)
            .ToList();

        return new OrgAttendanceOverviewDto
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            OverallPercent = AttendanceWeights.Percent(rows.Select(r => r.Status)),
            TotalSessions = rows.Select(r => r.AttendanceSessionId).Distinct().Count(),
            TotalRecords = rows.Count,
            Branches = branches,
        };
    }
}

// --------------------- One branch's per-course breakdown (drill-down) ---------------------

public record GetBranchAttendanceDetailQuery(Guid BranchId, DateOnly? FromDate, DateOnly? ToDate)
    : IRequest<BranchAttendanceDetailDto>;

public class GetBranchAttendanceDetailQueryHandler
    : IRequestHandler<GetBranchAttendanceDetailQuery, BranchAttendanceDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBranchAttendanceDetailQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BranchAttendanceDetailDto> Handle(GetBranchAttendanceDetailQuery request, CancellationToken ct)
    {
        var orgId = OrgAdminScope.RequireOrganizationId(_currentUser);

        var branch = await _db.Branches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BranchId && b.OrganizationId == orgId, ct)
            ?? throw new KeyNotFoundException("Branch was not found in your organization.");

        var q = _db.AttendanceRecords.AsNoTracking()
            .Where(r => r.BranchId == request.BranchId
                        && r.Session.OrganizationId == orgId
                        && r.Session.Status != AttendanceSessionStatus.Cancelled);
        if (request.FromDate is not null) q = q.Where(r => r.SessionDate >= request.FromDate.Value);
        if (request.ToDate is not null) q = q.Where(r => r.SessionDate <= request.ToDate.Value);

        var rows = await q
            .Select(r => new
            {
                r.CourseId,
                CourseTitle = r.Session.Course.Title,
                TeacherFirst = r.Session.Course.Teacher.FirstName,
                TeacherLast = r.Session.Course.Teacher.LastName,
                r.StudentId,
                r.AttendanceSessionId,
                r.Status,
            })
            .ToListAsync(ct);

        var courses = rows
            .GroupBy(r => new { r.CourseId, r.CourseTitle, r.TeacherFirst, r.TeacherLast })
            .Select(g =>
            {
                var statuses = g.Select(x => x.Status).ToList();
                return new CourseAttendanceRowDto
                {
                    CourseId = g.Key.CourseId,
                    CourseTitle = g.Key.CourseTitle,
                    TeacherName = (g.Key.TeacherFirst + " " + g.Key.TeacherLast).Trim(),
                    SessionCount = g.Select(x => x.AttendanceSessionId).Distinct().Count(),
                    StudentCount = g.Select(x => x.StudentId).Distinct().Count(),
                    AttendancePercent = AttendanceWeights.Percent(statuses),
                };
            })
            .OrderBy(c => c.CourseTitle)
            .ToList();

        return new BranchAttendanceDetailDto
        {
            BranchId = branch.Id,
            BranchName = branch.Name,
            OverallPercent = AttendanceWeights.Percent(rows.Select(r => r.Status)),
            Courses = courses,
        };
    }
}
