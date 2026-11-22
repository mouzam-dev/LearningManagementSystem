using System.Net;
using FluentAssertions;
using LMS.Application.Common;
using LMS.Application.Devices;
using LMS.Domain.Entities;
using LMS.Infrastructure.Persistence;
using LMS.WebAPI.Services.Push;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LMS.Tests;

public class PushTests
{
    private static ApplicationDbContext Db(PushQueue queue) => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .AddInterceptors(new NotificationPushInterceptor(queue))
        .Options);

    private static async Task<List<PushMessage>> Drain(PushQueue q)
    {
        var got = new List<PushMessage>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        try { await foreach (var m in q.ReadAllAsync(cts.Token)) got.Add(m); }
        catch (OperationCanceledException) { }
        return got;
    }

    [Fact]
    public async Task Saved_notification_is_queued_as_a_push()
    {
        var q = new PushQueue();
        await using var db = Db(q);
        var user = Guid.NewGuid();
        db.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = user, Type = "x", Title = "Quiz graded", Message = "You scored 9/10", CreatedAt = DateTime.UtcNow });
        (await Drain(q)).Should().BeEmpty("nothing is pushed before the save commits");

        await db.SaveChangesAsync();

        (await Drain(q)).Should().ContainSingle().Which.Should().Be(new PushMessage(user, "Quiz graded", "You scored 9/10"));
    }

    [Fact]
    public async Task Other_saves_queue_nothing()
    {
        var q = new PushQueue();
        await using var db = Db(q);
        db.PageViews.Add(new PageView { VisitorId = Guid.NewGuid(), Path = "/", Device = "mobile", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        (await Drain(q)).Should().BeEmpty();
    }

    [Fact]
    public async Task Register_repoints_an_existing_token_to_the_new_user()
    {
        await using var db = Db(new PushQueue());
        var (a, b) = (Guid.NewGuid(), Guid.NewGuid());
        var current = new Mock<ICurrentUserService>();
        var handler = new DeviceCommandHandlers(db, current.Object);

        current.Setup(c => c.GetUserId()).Returns(a);
        await handler.Handle(new RegisterDeviceCommand("tok-1", "Android"), default);
        current.Setup(c => c.GetUserId()).Returns(b);
        await handler.Handle(new RegisterDeviceCommand("tok-1", null), default);

        var rows = await db.DeviceTokens.ToListAsync();
        rows.Should().ContainSingle();
        rows[0].UserId.Should().Be(b);
        rows[0].Platform.Should().Be("android");

        current.Setup(c => c.GetUserId()).Returns(a);
        await handler.Handle(new UnregisterDeviceCommand("tok-1"), default);
        (await db.DeviceTokens.CountAsync()).Should().Be(1, "only the token's owner can remove it");
        current.Setup(c => c.GetUserId()).Returns(b);
        await handler.Handle(new UnregisterDeviceCommand("tok-1"), default);
        (await db.DeviceTokens.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "{}", true)]
    [InlineData(HttpStatusCode.BadRequest, "{\"error\":{\"details\":[{\"errorCode\":\"UNREGISTERED\"}]}}", true)]
    [InlineData(HttpStatusCode.BadRequest, "The registration token is not a valid FCM registration token", true)]
    [InlineData(HttpStatusCode.Unauthorized, "{}", false)]
    [InlineData(HttpStatusCode.InternalServerError, "{}", false)]
    public void Dead_tokens_are_recognised(HttpStatusCode status, string body, bool dead)
        => FcmPushSender.IsDeadToken(status, body).Should().Be(dead);
}
