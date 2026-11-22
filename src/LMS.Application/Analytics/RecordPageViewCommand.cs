using System.Text.RegularExpressions;
using LMS.Application.Common;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Analytics;

/// <summary>
/// Records one page visit. Anonymous (visitors count too); the signed-in user and role come from the
/// token, never from the request body. Invalid paths are dropped silently - this is telemetry.
/// </summary>
public record RecordPageViewCommand(Guid VisitorId, string? Path, string? UserAgent) : IRequest;

public class RecordPageViewCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<RecordPageViewCommand>
{
    public async Task Handle(RecordPageViewCommand request, CancellationToken ct)
    {
        var path = PagePath.Normalize(request.Path);
        if (path is null || request.VisitorId == Guid.Empty) return;
        // Super admins checking the site would skew the numbers they are here to read.
        if (currentUser.IsAuthenticated && currentUser.GetRole() == "SuperAdmin") return;

        db.PageViews.Add(new PageView
        {
            VisitorId = request.VisitorId,
            UserId = currentUser.IsAuthenticated ? currentUser.GetUserId() : null,
            Role = currentUser.IsAuthenticated ? currentUser.GetRole() : null,
            Path = path,
            Device = PagePath.Device(request.UserAgent),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}

public static partial class PagePath
{
    /// <summary>
    /// "/teacher/courses/89ca2f72-...?tab=1" -> "/teacher/courses/:id". Ids (GUIDs, numbers, long codes
    /// with digits such as LMS-4C5301A6) collapse so every course / user counts as one page.
    /// Returns null for anything that isn't an app path.
    /// </summary>
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var p = raw.Trim();
        var q = p.IndexOfAny(['?', '#']);
        if (q >= 0) p = p[..q];
        if (!p.StartsWith('/') || p.StartsWith("//") || p.Length > 300) return null;

        var parts = p.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s, out _) || s.All(char.IsDigit) || (s.Length >= 10 && s.Any(char.IsDigit)) ? ":id" : s.ToLowerInvariant())
            .ToList();
        if (parts.Any(s => !SafeSegment().IsMatch(s))) return null;
        var result = "/" + string.Join('/', parts);
        return result.Length <= 200 ? result : null;
    }

    public static string Device(string? userAgent)
    {
        var ua = userAgent ?? string.Empty;
        if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) || ua.Contains("Tablet", StringComparison.OrdinalIgnoreCase)) return "tablet";
        if (ua.Contains("Mobi", StringComparison.OrdinalIgnoreCase)) return "mobile";
        // Android phones say "Mobile"; Android without it is a tablet.
        return ua.Contains("Android", StringComparison.OrdinalIgnoreCase) ? "tablet" : "desktop";
    }

    [GeneratedRegex(@"^(:id|[a-z0-9._-]{1,60})$")]
    private static partial Regex SafeSegment();
}
