using System.Text.Json;
using FluentAssertions;
using LMS.Application.Arabic;
using LMS.Application.Common;
using LMS.Domain.Entities;
using LMS.Infrastructure.Persistence;
using LMS.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LMS.Tests;

public class ArabicTests
{
    private static ApplicationDbContext NewDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task SaveProgress_keeps_best_score_and_counts_attempts()
    {
        await using var db = NewDb();
        db.ArabicUnits.Add(new ArabicUnit { Id = "b1u01", BookNumber = 1, UnitNumber = 1,
            Lessons = { new ArabicLesson { Number = 1 } } });
        await db.SaveChangesAsync();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetUserId()).Returns(Guid.NewGuid());
        var handler = new SaveArabicProgressCommandHandler(db, user.Object);

        await handler.Handle(new SaveArabicProgressCommand("b1u01", 1, 80), default);
        var r = await handler.Handle(new SaveArabicProgressCommand("b1u01", 1, 40), default);

        r.BestScore.Should().Be(80);
        r.Attempts.Should().Be(2);
        db.ArabicProgress.Should().HaveCount(1);
        await handler.Invoking(h => h.Handle(new SaveArabicProgressCommand("b1u01", 9, 50), default))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void Seeder_maps_unit_json_and_skips_invalid_questions()
    {
        const string json = """
        {"id":"b1u01","book":1,"unit":1,"title":{"ar":"ت","en":"Greetings","ur":"Salaam"},
         "lessons":[{"n":1,"title":{"ar":"a","en":"b","ur":"c"},"intro":{"ar":"","en":"i","ur":"j"},
           "dialogue":[{"who":"خالد","ar":"السلام","tr":"as-salam","en":"Peace","ur":"Salamati"}],
           "vocab":[{"ar":"بيت","tr":"bayt","en":"house","ur":"ghar"}],
           "quiz":[{"q":{"ar":"","en":"ok?","ur":"ok?"},"options":["x","y"],"options_ar":["س","ص"],"answer":1},
                   {"q":{"ar":"","en":"bad","ur":"bad"},"options":["x","y"],"answer":5}]}]}
        """;
        var dto = JsonSerializer.Deserialize<ArabicContentSeeder.UnitJson>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var unit = ArabicContentSeeder.ToEntity(dto, NullLogger.Instance);

        unit.TitleEn.Should().Be("Greetings");
        var lesson = unit.Lessons.Single();
        lesson.DialogueLines.Single().Speaker.Should().Be("خالد");
        lesson.VocabItems.Single().En.Should().Be("house");
        var q = lesson.QuizQuestions.Single();
        q.AnswerIndex.Should().Be(1);
        q.OptionsAr.Should().Equal("س", "ص");
    }
}
