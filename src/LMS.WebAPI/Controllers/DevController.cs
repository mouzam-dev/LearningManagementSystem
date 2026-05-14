using System.Text.Json;
using LMS.Application.Common;
using LMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebAPI.Controllers;

/// <summary>
/// Dev-only utilities. Routes here are only registered when the host is running
/// in the Development environment (see Program.cs).
/// </summary>
[ApiController]
[Route("api/dev")]
[AllowAnonymous]
public class DevController : ControllerBase
{
    private const string DemoTeacherEmail = "demo.teacher@lms.dev";
    private const string DemoTeacherPassword = "Password1!";
    private const string DemoAdminEmail = "demo.admin@lms.dev";
    private const string DemoAdminPassword = "Password1!";
    private const string DemoStudentEmail = "demo.student@lms.dev";
    private const string DemoStudentPassword = "Password1!";

    // Public-domain Blender Foundation films, hosted on Google's CDN. Cycled
    // across lessons so every course has a working playable video.
    private static readonly string[] SampleVideoUrls =
    [
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4",
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4",
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/TearsOfSteel.mp4",
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4",
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerEscapes.mp4",
    ];

    private readonly IApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public DevController(IApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Idempotently plant a demo teacher, a handful of published courses across a
    /// few categories, and a 2-module / 6-lesson syllabus for every course that
    /// doesn't have one yet. Re-running is safe — only missing pieces are added.
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        // Belt-and-suspenders — the route should only be registered in Development,
        // but if that ever drifts, this guard prevents accidental seeding in prod.
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;

        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Email == DemoTeacherEmail, ct);
        if (teacher is null)
        {
            teacher = new User
            {
                Id = Guid.NewGuid(),
                Email = DemoTeacherEmail,
                FirstName = "Ada",
                LastName = "Lovelace",
                Role = "Teacher",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoTeacherPassword, workFactor: 12),
                Bio = "Demo teacher for catalog seed data.",
                IsVerified = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.Users.Add(teacher);
        }

        // Idempotent — admin demo account so the Admin module is reachable without
        // having to register through the public form (which only allows Student/Teacher).
        var adminExists = await _db.Users.AnyAsync(u => u.Email == DemoAdminEmail, ct);
        if (!adminExists)
        {
            _db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = DemoAdminEmail,
                FirstName = "Admin",
                LastName = "Root",
                Role = "Admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoAdminPassword, workFactor: 12),
                Bio = "Demo admin for the Admin module.",
                IsVerified = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        var demoCourses = new[]
        {
            new { Title = "Modern Angular: Signals, Standalone, and Beyond", Category = "Programming",
                  Description = "Build production Angular 20 apps with the latest reactive primitives, standalone components, and the new control-flow syntax.",
                  Thumbnail = (string?)null, MaxStudents = (int?)null },
            new { Title = "Clean Architecture in .NET 8",                    Category = "Programming",
                  Description = "Layered design, CQRS with MediatR, and EF Core best-practices on .NET 8 — the same patterns used in this LMS.",
                  Thumbnail = (string?)null, MaxStudents = (int?)200 },
            new { Title = "Designing for Trust: UI Foundations",             Category = "Design",
                  Description = "Type, color, and motion principles for software interfaces that feel both modern and reassuring.",
                  Thumbnail = (string?)null, MaxStudents = (int?)null },
            new { Title = "Product Analytics Fundamentals",                  Category = "Data Science",
                  Description = "Funnels, cohorts, and A/B tests — measure what matters and avoid common attribution traps.",
                  Thumbnail = (string?)null, MaxStudents = (int?)50 },
            new { Title = "Go-to-Market for Solo Builders",                  Category = "Business",
                  Description = "From landing page to first paying customer: positioning, distribution, and pricing for one-person teams.",
                  Thumbnail = (string?)null, MaxStudents = (int?)null },
            new { Title = "TypeScript Deep Dive",                            Category = "Programming",
                  Description = "Generics, conditional types, and inference tricks that make large TS codebases easier to live with.",
                  Thumbnail = (string?)null, MaxStudents = (int?)null },
        };

        var teacherCourses = await _db.Courses
            .Where(c => c.TeacherId == teacher.Id)
            .ToListAsync(ct);

        var coursesAdded = 0;
        foreach (var c in demoCourses)
        {
            if (teacherCourses.Any(existing => existing.Title == c.Title)) continue;

            var course = new Course
            {
                Id = Guid.NewGuid(),
                TeacherId = teacher.Id,
                Title = c.Title,
                Description = c.Description,
                Category = c.Category,
                ThumbnailUrl = c.Thumbnail,
                MaxStudents = c.MaxStudents,
                IsPublished = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.Courses.Add(course);
            teacherCourses.Add(course);
            coursesAdded++;
        }

        if (coursesAdded > 0)
        {
            // Save courses now so subsequent module/lesson lookups can find them.
            await _db.SaveChangesAsync(ct);
        }

        // For every demo course that doesn't have any modules yet, plant a
        // syllabus. Keeps the seed fully idempotent.
        var modulesAdded = 0;
        var lessonsAdded = 0;
        var videoIndex = 0;

        foreach (var course in teacherCourses)
        {
            var hasModules = await _db.Modules.AnyAsync(m => m.CourseId == course.Id, ct);
            if (hasModules) continue;

            var syllabus = new[]
            {
                new
                {
                    Title = "Getting started",
                    Description = "Set the stage and get your environment ready.",
                    Lessons = new[]
                    {
                        new { Title = "Welcome and overview", Duration = 5 },
                        new { Title = "Setting up your environment", Duration = 8 },
                        new { Title = "Your first hands-on", Duration = 6 },
                    },
                },
                new
                {
                    Title = "Core concepts",
                    Description = "Build a working mental model — patterns, pitfalls, practice.",
                    Lessons = new[]
                    {
                        new { Title = "Fundamentals deep-dive", Duration = 12 },
                        new { Title = "Patterns and anti-patterns", Duration = 15 },
                        new { Title = "Hands-on lab", Duration = 10 },
                    },
                },
            };

            for (var mIdx = 0; mIdx < syllabus.Length; mIdx++)
            {
                var ms = syllabus[mIdx];
                var module = new Module
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Title = ms.Title,
                    Description = ms.Description,
                    Order = mIdx + 1,
                    CreatedAt = now,
                };
                _db.Modules.Add(module);
                modulesAdded++;

                for (var lIdx = 0; lIdx < ms.Lessons.Length; lIdx++)
                {
                    var ls = ms.Lessons[lIdx];
                    var videoUrl = SampleVideoUrls[videoIndex++ % SampleVideoUrls.Length];

                    _db.Lessons.Add(new Lesson
                    {
                        Id = Guid.NewGuid(),
                        ModuleId = module.Id,
                        Title = ls.Title,
                        Type = "Video",
                        Content = JsonSerializer.Serialize(new { videoUrl }),
                        Duration = ls.Duration,
                        Order = lIdx + 1,
                        IsPublished = true,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                    lessonsAdded++;
                }
            }
        }

        // Plant assessments (1 quiz + 1 assignment per course) for any course
        // that doesn't have any yet. Same idempotent pattern.
        var assessmentsAdded = 0;
        var questionsAdded = 0;

        foreach (var course in teacherCourses)
        {
            var hasAssessments = await _db.Assessments.AnyAsync(a => a.CourseId == course.Id, ct);
            if (hasAssessments) continue;

            var quiz = new Assessment
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Knowledge check",
                Type = "Quiz",
                TimeLimit = 10,
                PassingScore = 75,
                MaxAttempts = 3,
                CreatedAt = now,
            };
            _db.Assessments.Add(quiz);
            assessmentsAdded++;

            var quizQuestions = new[]
            {
                new
                {
                    Text = "How is the course content structured in this LMS?",
                    Type = "MCQ",
                    Options = new[] { "One long video", "Modules with multiple lessons", "PDF handouts only", "Live sessions" },
                    Correct = "Modules with multiple lessons",
                    Points = 1,
                },
                new
                {
                    Text = "Watching a lesson video to the end automatically marks it as complete.",
                    Type = "TrueFalse",
                    Options = new[] { "True", "False" },
                    Correct = "True",
                    Points = 1,
                },
                new
                {
                    Text = "Where can you see your overall progress across enrolled courses?",
                    Type = "MCQ",
                    Options = new[] { "Catalog", "Dashboard", "Settings", "Inbox" },
                    Correct = "Dashboard",
                    Points = 1,
                },
                new
                {
                    Text = "Which page lets you discover and enrol in new courses?",
                    Type = "MCQ",
                    Options = new[] { "Catalog", "Dashboard", "Profile", "Help" },
                    Correct = "Catalog",
                    Points = 1,
                },
            };

            for (var qIdx = 0; qIdx < quizQuestions.Length; qIdx++)
            {
                var q = quizQuestions[qIdx];
                _db.Questions.Add(new Question
                {
                    Id = Guid.NewGuid(),
                    AssessmentId = quiz.Id,
                    QuestionText = q.Text,
                    Type = q.Type,
                    Options = JsonSerializer.Serialize(q.Options),
                    CorrectAnswer = q.Correct,
                    Points = q.Points,
                    Order = qIdx + 1,
                });
                questionsAdded++;
            }

            // Assignment with a due date a week out so the dashboard's
            // upcoming-deadlines panel has something to show.
            _db.Assessments.Add(new Assessment
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                Title = "Course reflection",
                Type = "Assignment",
                PassingScore = 70,
                DueDate = now.AddDays(7),
                MaxAttempts = 1,
                CreatedAt = now,
            });
            assessmentsAdded++;
        }

        await _db.SaveChangesAsync(ct);

        // Idempotent — demo student account so all three roles have a consistent
        // sign-in. Auto-enrolled in the first two demo courses so the student
        // experience (dashboard, continue-learning) has data out of the box.
        var enrollmentsAdded = 0;
        var student = await _db.Users.FirstOrDefaultAsync(u => u.Email == DemoStudentEmail, ct);
        if (student is null)
        {
            student = new User
            {
                Id = Guid.NewGuid(),
                Email = DemoStudentEmail,
                FirstName = "Sam",
                LastName = "Learner",
                Role = "Student",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoStudentPassword, workFactor: 12),
                Bio = "Demo student for exploring the learner experience.",
                IsVerified = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.Users.Add(student);
            await _db.SaveChangesAsync(ct);
        }

        var demoEnrolCourses = await _db.Courses
            .Where(c => c.TeacherId == teacher.Id && c.IsPublished)
            .OrderBy(c => c.CreatedAt)
            .Take(2)
            .ToListAsync(ct);
        foreach (var course in demoEnrolCourses)
        {
            var alreadyEnrolled = await _db.Enrollments
                .AnyAsync(e => e.StudentId == student.Id && e.CourseId == course.Id, ct);
            if (alreadyEnrolled) continue;

            _db.Enrollments.Add(new Enrollment
            {
                Id = Guid.NewGuid(),
                StudentId = student.Id,
                CourseId = course.Id,
                EnrolledAt = now,
                ProgressPercentage = 0m,
                IsCompleted = false,
            });
            enrollmentsAdded++;
        }
        await _db.SaveChangesAsync(ct);

        var totalCourses = await _db.Courses.CountAsync(ct);
        var totalLessons = await _db.Lessons.CountAsync(ct);
        var totalAssessments = await _db.Assessments.CountAsync(ct);
        return Ok(new
        {
            teacherId = teacher.Id,
            teacherEmail = teacher.Email,
            studentEmail = student.Email,
            coursesAdded,
            modulesAdded,
            lessonsAdded,
            assessmentsAdded,
            questionsAdded,
            enrollmentsAdded,
            totalPublishedCourses = totalCourses,
            totalLessons,
            totalAssessments,
        });
    }
}
