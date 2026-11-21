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
    public async Task TeacherProgress_lists_only_own_students_with_their_totals()
    {
        await using var db = NewDb();
        User NewUser(string first, string role) => new() { Id = Guid.NewGuid(), FirstName = first, LastName = "X", Email = $"{first}@t.dev", Role = role };
        var teacher = NewUser("teach", "Teacher");
        var other = NewUser("other", "Teacher");
        var active = NewUser("active", "Student");
        var idle = NewUser("idle", "Student");
        var stranger = NewUser("stranger", "Student"); // enrolled only with the other teacher
        db.Users.AddRange(teacher, other, active, idle, stranger);
        var mine = new Course { Id = Guid.NewGuid(), Title = "Mine", TeacherId = teacher.Id };
        var theirs = new Course { Id = Guid.NewGuid(), Title = "Theirs", TeacherId = other.Id };
        db.Courses.AddRange(mine, theirs);
        db.Enrollments.AddRange(
            new Enrollment { Id = Guid.NewGuid(), CourseId = mine.Id, StudentId = active.Id },
            new Enrollment { Id = Guid.NewGuid(), CourseId = mine.Id, StudentId = idle.Id },
            new Enrollment { Id = Guid.NewGuid(), CourseId = theirs.Id, StudentId = stranger.Id });
        db.ArabicUnits.Add(new ArabicUnit { Id = "b1u02", BookNumber = 1, UnitNumber = 2,
            Lessons = { new ArabicLesson { Number = 1 }, new ArabicLesson { Number = 2 } } });
        db.ArabicProgress.AddRange(
            new ArabicProgress { UserId = active.Id, UnitId = "b1u02", LessonNumber = 1, BestScore = 100, Attempts = 2, UpdatedAt = DateTime.UtcNow.AddDays(-1) },
            new ArabicProgress { UserId = active.Id, UnitId = "b1u02", LessonNumber = 2, BestScore = 50, Attempts = 1, UpdatedAt = DateTime.UtcNow },
            new ArabicProgress { UserId = active.Id, UnitId = "b1u02", LessonNumber = 0, BestScore = 80, Attempts = 1, UpdatedAt = DateTime.UtcNow.AddDays(-2) },
            new ArabicProgress { UserId = stranger.Id, UnitId = "b1u02", LessonNumber = 1, BestScore = 90, Attempts = 1, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.GetUserId()).Returns(teacher.Id);

        var r = await new GetTeacherArabicProgressQueryHandler(db, user.Object).Handle(new GetTeacherArabicProgressQuery(null), default);

        r.TotalLessons.Should().Be(2);
        r.Students.TotalCount.Should().Be(2);
        r.Students.Items.Select(s => s.Email).Should().Equal("active@t.dev", "idle@t.dev"); // active first, stranger excluded
        var a = r.Students.Items[0];
        (a.LessonsDone, a.UnitTestsDone, a.AverageScore, a.Attempts).Should().Be((2, 1, 75, 4));
        (a.LastBook, a.LastUnit, a.LastLesson).Should().Be((1, 2, 2));
        r.Students.Items[1].LastActivity.Should().BeNull();
    }

    [Fact]
    public async Task Seeder_reimports_lessons_of_emptied_units_and_keeps_progress()
    {
        await using var db = NewDb();
        var userId = Guid.NewGuid();
        db.ArabicUnits.Add(new ArabicUnit { Id = "b1u01", BookNumber = 1, UnitNumber = 1, TitleEn = "old title" }); // lessons deleted
        db.ArabicProgress.Add(new ArabicProgress { UserId = userId, UnitId = "b1u01", LessonNumber = 1, BestScore = 80, Attempts = 1 });
        await db.SaveChangesAsync();

        await ArabicContentSeeder.SeedAsync(db, NullLogger.Instance);

        db.ArabicUnits.Should().HaveCount(65);
        var unit = db.ArabicUnits.Include(u => u.Lessons).ThenInclude(l => l.DialogueLines).Single(u => u.Id == "b1u01");
        unit.TitleEn.Should().NotBe("old title");
        unit.Lessons.Should().NotBeEmpty();
        db.ArabicDialogueLines.Should().Contain(d => d.Source == "quran");
        db.ArabicDialogueLines.Should().OnlyContain(d => d.Audio != null && d.Audio.EndsWith(".mp3"));
        db.ArabicVocabItems.Should().OnlyContain(v => v.Audio != null);
        db.ArabicProgress.Single().BestScore.Should().Be(80);

        var lessons = db.ArabicLessons.Count();
        await ArabicContentSeeder.SeedAsync(db, NullLogger.Instance); // second start: nothing to do
        db.ArabicLessons.Count().Should().Be(lessons);
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
