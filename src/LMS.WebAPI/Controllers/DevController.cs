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

    private readonly IApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public DevController(IApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Idempotently plant a demo teacher and a handful of published courses across
    /// a few categories so the catalog has something to show on a fresh database.
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

        var existingTitles = await _db.Courses
            .Where(c => c.TeacherId == teacher.Id)
            .Select(c => c.Title)
            .ToListAsync(ct);

        var added = 0;
        foreach (var c in demoCourses)
        {
            if (existingTitles.Contains(c.Title)) continue;

            _db.Courses.Add(new Course
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
            });
            added++;
        }

        await _db.SaveChangesAsync(ct);

        var totalCourses = await _db.Courses.CountAsync(ct);
        return Ok(new
        {
            teacherId = teacher.Id,
            teacherEmail = teacher.Email,
            coursesAdded = added,
            totalPublishedCourses = totalCourses,
        });
    }
}
