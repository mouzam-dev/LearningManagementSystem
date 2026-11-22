using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using Google.Apis.Auth.OAuth2;
using LMS.Domain.Entities;
using LMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LMS.WebAPI.Services.Push;

public record PushMessage(Guid UserId, string Title, string Body);

/// <summary>In-memory hand-off from request threads to the sender. Lost on restart, which is fine for pushes.</summary>
public class PushQueue
{
    private readonly Channel<PushMessage> _channel = Channel.CreateBounded<PushMessage>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropOldest });

    public void Enqueue(PushMessage m) => _channel.Writer.TryWrite(m);
    public IAsyncEnumerable<PushMessage> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

/// <summary>
/// Every in-app Notification row becomes a phone push once it is committed. Hooking SaveChanges
/// (rather than INotificationService) means a rolled-back command never pushes.
/// </summary>
public class NotificationPushInterceptor(PushQueue queue) : SaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, List<PushMessage>> _pending = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Capture(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Flush(eventData.Context);
        return result;
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
    {
        Flush(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pending.Remove(eventData.Context!);

    private void Capture(DbContext? ctx)
    {
        if (ctx is null) return;
        var added = ctx.ChangeTracker.Entries<Notification>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => new PushMessage(e.Entity.UserId, e.Entity.Title, e.Entity.Message))
            .ToList();
        if (added.Count > 0) _pending.AddOrUpdate(ctx, added);
    }

    private void Flush(DbContext? ctx)
    {
        if (ctx is null || !_pending.TryGetValue(ctx, out var list)) return;
        _pending.Remove(ctx);
        foreach (var m in list) queue.Enqueue(m);
    }
}

/// <summary>
/// Sends queued pushes through Firebase Cloud Messaging (HTTP v1). Needs the Firebase service
/// account JSON in Firebase__ServiceAccountJson; without it pushes are skipped (in-app
/// notifications still work). Tokens Firebase reports as gone are deleted.
/// </summary>
public class FcmPushSender(
    PushQueue queue,
    IServiceScopeFactory scopes,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<FcmPushSender> log) : BackgroundService
{
    private const string Scope = "https://www.googleapis.com/auth/firebase.messaging";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var json = config["Firebase:ServiceAccountJson"];
        ITokenAccess? credential = null;
        string? projectId = null;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                projectId = JsonDocument.Parse(json).RootElement.GetProperty("project_id").GetString();
#pragma warning disable CS0618 // FromJson is fine for our own, trusted service-account secret
                credential = GoogleCredential.FromJson(json).CreateScoped(Scope);
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                log.LogError(ex, "[push] Firebase:ServiceAccountJson is not a valid service-account key; pushes disabled");
            }
        }
        if (credential is null || string.IsNullOrEmpty(projectId))
            log.LogInformation("[push] Firebase not configured; phone pushes are off");

        await foreach (var m in queue.ReadAllAsync(stoppingToken))
        {
            if (credential is null) continue;
            try { await SendAsync(m, credential, projectId!, stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogWarning(ex, "[push] send failed for user {UserId}", m.UserId);
            }
        }
    }

    private async Task SendAsync(PushMessage m, ITokenAccess credential, string projectId, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokens = await db.DeviceTokens.Where(t => t.UserId == m.UserId).ToListAsync(ct);
        if (tokens.Count == 0) return;

        var http = httpFactory.CreateClient("fcm");
        var bearer = await credential.GetAccessTokenForRequestAsync(cancellationToken: ct);
        var url = $"https://fcm.googleapis.com/v1/projects/{projectId}/messages:send";
        var gone = new List<DeviceToken>();
        foreach (var t in tokens)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(BuildMessage(t.Token, m)),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            using var res = await http.SendAsync(req, ct);
            if (res.IsSuccessStatusCode) continue;
            var body = await res.Content.ReadAsStringAsync(ct);
            if (IsDeadToken(res.StatusCode, body)) gone.Add(t);
            else log.LogWarning("[push] FCM {Status}: {Body}", (int)res.StatusCode, body.Length > 300 ? body[..300] : body);
        }
        if (gone.Count > 0)
        {
            db.DeviceTokens.RemoveRange(gone);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>FCM v1 payload. Tapping the notification opens the app's notifications page.</summary>
    public static object BuildMessage(string token, PushMessage m) => new
    {
        message = new
        {
            token,
            notification = new { title = Trim(m.Title, 100), body = Trim(m.Body, 300) },
            data = new Dictionary<string, string> { ["path"] = "/notifications" },
            android = new { priority = "high" },
        },
    };

    /// <summary>404 / UNREGISTERED: app uninstalled or token rotated. 400 INVALID_ARGUMENT on the token: never valid.</summary>
    public static bool IsDeadToken(HttpStatusCode status, string body) =>
        status == HttpStatusCode.NotFound
        || body.Contains("UNREGISTERED", StringComparison.Ordinal)
        || (status == HttpStatusCode.BadRequest && body.Contains("registration token", StringComparison.OrdinalIgnoreCase));

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
