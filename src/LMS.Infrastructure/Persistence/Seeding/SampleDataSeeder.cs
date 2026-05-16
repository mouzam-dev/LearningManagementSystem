using System.Text.Json;
using LMS.Domain.Constants;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent demo-content seeder for development. Runs after <see cref="TenancySeeder"/>
/// and only on a "fresh" database (no Course rows) — once any course exists we assume
/// real teacher/admin work is in progress and don't touch it.
///
/// Produces a realistic, screenshot-ready dataset:
///   - 3 organizations (Pioneer Tech Academy, Global Business School, CodeForge Bootcamp)
///   - 2 branches per org (HQ + a secondary location)
///   - 1 OrgAdmin, 5 teachers, ~20 students per org (~80 users total)
///   - 5 courses per org (15 total) with 3 modules / 4–5 lessons / 1–2 assessments each
///   - ~150 enrollments across students with mixed progress + a handful of completions
///   - Certificates issued for completions; lesson progress + submissions populated
///   - Course announcements pinned for the latest module on each course
///
/// Every user password is "Password1!" to keep demo logins discoverable.
/// </summary>
public static class SampleDataSeeder
{
    private const string DemoPassword = "Password1!";
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct = default)
    {
        if (await db.Courses.AnyAsync(ct))
        {
            logger.LogInformation("SampleDataSeeder: courses already exist, skipping.");
            return;
        }

        logger.LogInformation("SampleDataSeeder: empty catalog detected, generating demo data…");
        var rng = new Random(424242); // deterministic so reruns give the same dataset

        var orgs = await SeedOrganizationsAsync(db, ct);
        var users = await SeedUsersForOrgsAsync(db, orgs, ct);
        var courses = await SeedCoursesAsync(db, orgs, users, rng, ct);
        await SeedEnrollmentsAndProgressAsync(db, courses, users, rng, ct);

        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "SampleDataSeeder: done — {Orgs} orgs, {Users} users, {Courses} courses.",
            orgs.Count, users.AllUsers.Count, courses.Count);
    }

    // -------------------------------------------------------------------------
    // Organizations + branches
    // -------------------------------------------------------------------------

    private record OrgSeed(Guid Id, string Name, string Slug, string Description, Guid HqBranchId, Guid RemoteBranchId, string HqLocation, string RemoteLocation);

    private static async Task<List<OrgSeed>> SeedOrganizationsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var defs = new[]
        {
            new
            {
                Name = "Pioneer Tech Academy",
                Slug = "pioneer-tech",
                Description = "Hands-on training in modern software engineering — from fundamentals to production.",
                ContactEmail = "hello@pioneertech.example",
                HqLocation = "San Francisco, CA",
                Remote = ("EU Remote", "Berlin, DE"),
            },
            new
            {
                Name = "Global Business School",
                Slug = "global-business",
                Description = "Executive education for product, ops, and finance leaders across industries.",
                ContactEmail = "info@globalbusiness.example",
                HqLocation = "New York, NY",
                Remote = ("London Campus", "London, UK"),
            },
            new
            {
                Name = "CodeForge Bootcamp",
                Slug = "codeforge",
                Description = "Intensive, cohort-based bootcamps that take career switchers from zero to job-ready.",
                ContactEmail = "team@codeforge.example",
                HqLocation = "Austin, TX",
                Remote = ("Online Cohorts", "Worldwide"),
            },
        };

        var now = DateTime.UtcNow;
        var seeds = new List<OrgSeed>();

        foreach (var d in defs)
        {
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = d.Name,
                Slug = d.Slug,
                Description = d.Description,
                ContactEmail = d.ContactEmail,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Organizations.Add(org);

            var hq = new Branch
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Name = $"{d.Name} HQ",
                Code = "HQ",
                Location = d.HqLocation,
                ContactEmail = d.ContactEmail,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            var remote = new Branch
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                Name = d.Remote.Item1,
                Code = d.Remote.Item1.Contains("Remote") ? "EU" : "RMT",
                Location = d.Remote.Item2,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.Branches.Add(hq);
            db.Branches.Add(remote);

            seeds.Add(new OrgSeed(org.Id, d.Name, d.Slug, d.Description, hq.Id, remote.Id, hq.Location!, remote.Location!));
        }

        await db.SaveChangesAsync(ct);
        return seeds;
    }

    // -------------------------------------------------------------------------
    // Users: OrgAdmins, Teachers, Students per org. Names are realistic and
    // diverse; emails follow {first.last}@{slug}.example.
    // -------------------------------------------------------------------------

    private record SeededUsers(
        List<User> AllUsers,
        Dictionary<Guid, List<User>> TeachersByOrg,
        Dictionary<Guid, List<User>> StudentsByOrg);

    private static readonly (string First, string Last)[] TeacherPool =
    {
        ("Maya", "Patel"), ("Liam", "OConnor"), ("Aisha", "Khan"), ("Diego", "Alvarez"),
        ("Yuki", "Tanaka"), ("Noah", "Goldberg"), ("Priya", "Iyer"), ("Karim", "Hassan"),
        ("Sara", "Nielsen"), ("Marcus", "Johnson"), ("Lina", "Mueller"), ("Ravi", "Subramanian"),
        ("Sofia", "Rossi"), ("Wei", "Chen"), ("Emma", "Andersson"), ("Tomas", "Novak"),
    };

    private static readonly (string First, string Last)[] StudentPool =
    {
        ("Olivia", "Martinez"), ("Ethan", "Lee"), ("Ava", "Brown"), ("James", "Wilson"),
        ("Sophia", "Garcia"), ("Mason", "Davis"), ("Isabella", "Rodriguez"), ("Lucas", "Hernandez"),
        ("Mia", "Lopez"), ("Logan", "Gonzalez"), ("Charlotte", "Perez"), ("Elijah", "Sanchez"),
        ("Amelia", "Ramirez"), ("Benjamin", "Torres"), ("Harper", "Flores"), ("Henry", "Rivera"),
        ("Evelyn", "Gomez"), ("Alexander", "Diaz"), ("Abigail", "Reyes"), ("Jackson", "Cruz"),
        ("Emily", "Morales"), ("Sebastian", "Ortiz"), ("Elizabeth", "Gutierrez"), ("Jack", "Chavez"),
        ("Avery", "Ramos"), ("Owen", "Ruiz"), ("Sofia", "Vargas"), ("Wyatt", "Castillo"),
        ("Ella", "Romero"), ("John", "Mendoza"), ("Madison", "Herrera"), ("Carter", "Medina"),
        ("Scarlett", "Aguilar"), ("Luke", "Soto"), ("Victoria", "Vasquez"), ("Dylan", "Delgado"),
        ("Grace", "Contreras"), ("Levi", "Estrada"), ("Chloe", "Salazar"), ("Isaac", "Cervantes"),
        ("Penelope", "Ochoa"), ("Gabriel", "Padilla"), ("Layla", "Fuentes"), ("Julian", "Ibarra"),
        ("Riley", "Espinoza"), ("Mateo", "Cardenas"), ("Zoey", "Rios"), ("Anthony", "Maldonado"),
        ("Nora", "Carrillo"), ("Jaxon", "Velasquez"), ("Lily", "Munoz"), ("Lincoln", "Solis"),
        ("Eleanor", "Jimenez"), ("Joseph", "Pacheco"), ("Hannah", "Avila"), ("Andrew", "Galvan"),
        ("Lillian", "Camacho"), ("Adrian", "Becerra"), ("Addison", "Cabrera"), ("Christopher", "Tovar"),
    };

    private static async Task<SeededUsers> SeedUsersForOrgsAsync(
        ApplicationDbContext db, List<OrgSeed> orgs, CancellationToken ct)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 8);
        var now = DateTime.UtcNow;

        var allUsers = new List<User>();
        var teachersByOrg = new Dictionary<Guid, List<User>>();
        var studentsByOrg = new Dictionary<Guid, List<User>>();

        var teacherCursor = 0;
        var studentCursor = 0;

        // Platform-wide SuperAdmin so the manual's "log in as super admin" works.
        allUsers.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "demo.admin@lms.dev",
            FirstName = "Demo",
            LastName = "Admin",
            Role = Roles.SuperAdmin,
            PasswordHash = passwordHash,
            Bio = "Demo super admin (seeded).",
            IsVerified = true,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        });

        foreach (var org in orgs)
        {
            // OrgAdmin in HQ
            var adminEmail = $"admin@{org.Slug}.example";
            var orgAdmin = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                FirstName = org.Name.Split(' ')[0], // e.g. "Pioneer"
                LastName = "Admin",
                Role = Roles.OrgAdmin,
                PasswordHash = passwordHash,
                Bio = $"Organization administrator for {org.Name}.",
                IsVerified = true,
                IsActive = true,
                OrganizationId = org.Id,
                BranchId = org.HqBranchId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            allUsers.Add(orgAdmin);

            // 5 teachers per org, alternating branches
            var teachers = new List<User>();
            for (var i = 0; i < 5; i++)
            {
                var (first, last) = TeacherPool[teacherCursor++ % TeacherPool.Length];
                var emailLocal = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}".Replace("'", string.Empty);
                var t = new User
                {
                    Id = Guid.NewGuid(),
                    Email = $"{emailLocal}@{org.Slug}.example",
                    FirstName = first,
                    LastName = last,
                    Role = Roles.Teacher,
                    PasswordHash = passwordHash,
                    Bio = $"Instructor at {org.Name}.",
                    IsVerified = true,
                    IsActive = true,
                    OrganizationId = org.Id,
                    BranchId = i % 2 == 0 ? org.HqBranchId : org.RemoteBranchId,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                teachers.Add(t);
                allUsers.Add(t);
            }
            teachersByOrg[org.Id] = teachers;

            // 20 students per org, alternating branches
            var students = new List<User>();
            for (var i = 0; i < 20; i++)
            {
                var (first, last) = StudentPool[studentCursor++ % StudentPool.Length];
                var emailLocal = $"{first.ToLowerInvariant()}.{last.ToLowerInvariant()}";
                var s = new User
                {
                    Id = Guid.NewGuid(),
                    Email = $"{emailLocal}{(i / StudentPool.Length > 0 ? (i / StudentPool.Length).ToString() : string.Empty)}@{org.Slug}.example",
                    FirstName = first,
                    LastName = last,
                    Role = Roles.Student,
                    PasswordHash = passwordHash,
                    IsVerified = true,
                    IsActive = true,
                    OrganizationId = org.Id,
                    BranchId = i % 2 == 0 ? org.HqBranchId : org.RemoteBranchId,
                    CreatedAt = now.AddDays(-i),
                    UpdatedAt = now.AddDays(-i),
                };
                students.Add(s);
                allUsers.Add(s);
            }
            studentsByOrg[org.Id] = students;
        }

        // Friendly demo aliases — single-click logins documented in the manual.
        // Append a demo.* alias for each role inside the first org so the
        // existing TenancySeeder users (demo.teacher@lms.dev, etc.) keep working.
        var firstOrg = orgs[0];
        var firstHq = firstOrg.HqBranchId;
        var demoAliases = new[]
        {
            ("demo.teacher@lms.dev", "Demo", "Teacher", Roles.Teacher, (Guid?)firstHq),
            ("demo.orgadmin@lms.dev", "Demo", "OrgAdmin", Roles.OrgAdmin, (Guid?)firstHq),
            ("demo.student@lms.dev", "Demo", "Student", Roles.Student, (Guid?)firstHq),
        };
        foreach (var (email, first, last, role, branchId) in demoAliases)
        {
            allUsers.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = first,
                LastName = last,
                Role = role,
                PasswordHash = passwordHash,
                Bio = $"Demo {role.ToLowerInvariant()} login.",
                IsVerified = true,
                IsActive = true,
                OrganizationId = firstOrg.Id,
                BranchId = branchId,
                CreatedAt = now,
                UpdatedAt = now,
            });
            if (role == Roles.Teacher)
            {
                // Make sure the demo teacher shows up in the teacher-by-org list so
                // it can own at least one of the seeded courses.
                teachersByOrg[firstOrg.Id].Add(allUsers[^1]);
            }
            if (role == Roles.Student)
            {
                studentsByOrg[firstOrg.Id].Add(allUsers[^1]);
            }
        }

        db.Users.AddRange(allUsers);
        await db.SaveChangesAsync(ct);

        return new SeededUsers(allUsers, teachersByOrg, studentsByOrg);
    }

    // -------------------------------------------------------------------------
    // Courses, modules, lessons, assessments, questions, announcements
    // -------------------------------------------------------------------------

    private static readonly (string Title, string Category, string Description, string[] Modules, string[][] Lessons)[] CourseLibrary =
    {
        (
            "Modern Angular: Signals, Standalone, and Beyond",
            "Frontend",
            "Build production-grade Angular apps with Signals, standalone components, and the new application builder. We cover everything from project setup to deployment.",
            new[] { "Foundations", "Reactivity with Signals", "Routing + Forms", "Production Deploy" },
            new[]
            {
                new[] { "Welcome + setup", "TypeScript essentials", "Standalone components 101", "Project scaffolding" },
                new[] { "From RxJS to Signals", "Computed + effect", "State patterns", "Quiz: Signals" },
                new[] { "Routing fundamentals", "Reactive forms", "Form validation", "Lab: build a form" },
                new[] { "Build optimization", "Hosting options", "CI/CD basics" },
            }
        ),
        (
            "ASP.NET Core 8 from Scratch",
            "Backend",
            "From your first dotnet new to a production API: minimal APIs, controllers, EF Core, JWT auth, validation, and deployment.",
            new[] { "Project Setup", "Web API + EF Core", "Auth + Security", "Going to Prod" },
            new[]
            {
                new[] { "Why .NET 8", "Project layout", "Minimal API tour" },
                new[] { "Controllers vs endpoints", "EF Core mapping", "Migrations", "Quiz: EF Core" },
                new[] { "JWT auth", "Password hashing", "Authorization policies" },
                new[] { "Logging with Serilog", "Hosting on Azure", "Final project" },
            }
        ),
        (
            "Product Strategy for Engineers",
            "Product",
            "A practical guide to product thinking for engineers — discovery, sequencing, metrics, and stakeholder communication.",
            new[] { "Discovery", "Sequencing", "Metrics + Storytelling" },
            new[]
            {
                new[] { "What product is", "Customer interviews", "Synthesizing signal", "Quiz: discovery" },
                new[] { "MVP scoping", "Prioritization frameworks", "Roadmaps that ship" },
                new[] { "Choosing metrics", "Telling the story", "Final reflection" },
            }
        ),
        (
            "Foundations of Financial Accounting",
            "Finance",
            "The accounting concepts every business operator should know — debits, credits, the three statements, and how to read a 10-K.",
            new[] { "Concepts", "The Three Statements", "Reading Real Filings" },
            new[]
            {
                new[] { "Why accounting matters", "The accounting equation", "Debits and credits" },
                new[] { "Income statement", "Balance sheet", "Cash flow statement", "Quiz: statements" },
                new[] { "Reading a 10-K", "Red flags", "Capstone exercise" },
            }
        ),
        (
            "Negotiation for Operators",
            "Leadership",
            "Frameworks for negotiating with vendors, hires, and partners — built on real cases from operating teams.",
            new[] { "Mindset", "Preparation", "Live Negotiation" },
            new[]
            {
                new[] { "Why most negotiations fail", "Anchors and BATNA", "The ZOPA" },
                new[] { "Researching the counterparty", "Building leverage", "Quiz: prep" },
                new[] { "Opening offers", "Concessions", "Closing", "Capstone role-play" },
            }
        ),
        (
            "Full-Stack JavaScript: React + Node",
            "Frontend",
            "Build and ship a full-stack JavaScript app: React frontend, Express + Postgres backend, deployed end to end.",
            new[] { "Frontend Setup", "Backend API", "Database + Auth", "Ship It" },
            new[]
            {
                new[] { "Vite + React + TS", "Component architecture", "Styling choices" },
                new[] { "Express basics", "REST design", "Error handling", "Quiz: REST" },
                new[] { "Postgres + Prisma", "Sessions vs JWT", "Auth flows" },
                new[] { "Hosting frontend", "Hosting backend", "Final capstone" },
            }
        ),
        (
            "Cloud Engineering on AWS",
            "DevOps",
            "Hands-on AWS for engineers — IAM, VPC, EC2, S3, RDS, Lambda, and CloudFormation. Ship a multi-tier app on day one.",
            new[] { "AWS Basics", "Compute + Storage", "Networking + IAC" },
            new[]
            {
                new[] { "AWS account setup", "IAM 101", "Billing alarms" },
                new[] { "EC2 vs ECS vs Lambda", "S3 patterns", "RDS basics", "Quiz: services" },
                new[] { "VPC fundamentals", "CloudFormation tour", "Capstone deployment" },
            }
        ),
        (
            "Data Analysis with Python",
            "Data",
            "Pandas, NumPy, and matplotlib for analysts — from CSV to chart, with realistic datasets and reproducible notebooks.",
            new[] { "Python Essentials", "Pandas Deep Dive", "Visualization + Reporting" },
            new[]
            {
                new[] { "Jupyter setup", "Python data types", "Functions + scripts" },
                new[] { "DataFrames", "GroupBy + joins", "Cleaning messy data", "Quiz: pandas" },
                new[] { "matplotlib basics", "Storytelling with charts", "Final analysis" },
            }
        ),
        (
            "Intro to Machine Learning",
            "Data",
            "ML for engineers without a PhD — linear models, trees, evaluation, and pitfalls. Plenty of working code.",
            new[] { "ML Intuition", "Classic Algorithms", "Evaluation + Deployment" },
            new[]
            {
                new[] { "What ML is and isn't", "The bias/variance tradeoff", "Train / val / test" },
                new[] { "Linear regression", "Logistic regression", "Trees + ensembles", "Quiz: algorithms" },
                new[] { "Confusion matrices", "Cross-validation", "Serving a model" },
            }
        ),
        (
            "Effective Technical Writing",
            "Communication",
            "Writing for engineers — docs, RFCs, postmortems, and PR descriptions that get read and remembered.",
            new[] { "Foundations", "Document Types", "Practice" },
            new[]
            {
                new[] { "Why writing matters", "Plain language", "Structure first" },
                new[] { "Anatomy of a great RFC", "Postmortem patterns", "PR descriptions", "Quiz: structure" },
                new[] { "Editing your own work", "Getting feedback", "Capstone document" },
            }
        ),
        (
            "Design Systems in Practice",
            "Design",
            "How to build and maintain a real design system: tokens, primitives, composition, governance, and rollout strategy.",
            new[] { "Tokens + Primitives", "Composition", "Governance + Rollout" },
            new[]
            {
                new[] { "Why design systems", "Designing tokens", "Color + typography" },
                new[] { "Buttons, inputs, layout primitives", "Patterns and templates", "Documentation", "Quiz: composition" },
                new[] { "Versioning + adoption", "Working with engineering", "Capstone audit" },
            }
        ),
        (
            "Leading Engineering Teams",
            "Leadership",
            "From IC to TL to EM: practical advice on hiring, 1:1s, performance management, and shipping as a team.",
            new[] { "The IC → Lead Transition", "Operating a Team", "Scaling Yourself" },
            new[]
            {
                new[] { "What changes about your job", "Useful authority", "The first 90 days" },
                new[] { "1:1s that matter", "Career conversations", "Running rituals", "Quiz: rituals" },
                new[] { "Delegation patterns", "Hiring + interview design", "Final reflection" },
            }
        ),
        (
            "SQL for Analysts",
            "Data",
            "From SELECT to window functions: a practical SQL course for people who actually need to pull data.",
            new[] { "The Basics", "Joins + Aggregates", "Advanced + Performance" },
            new[]
            {
                new[] { "SELECT, WHERE, ORDER BY", "GROUP BY essentials", "Common pitfalls" },
                new[] { "JOINs explained", "Subqueries vs CTEs", "Window functions", "Quiz: windows" },
                new[] { "Indexes 101", "Reading EXPLAIN", "Capstone analysis" },
            }
        ),
        (
            "Marketing Analytics 101",
            "Marketing",
            "Set up, measure, and act on the channels that actually matter — without drowning in dashboards.",
            new[] { "Foundations", "Channels", "Reporting" },
            new[]
            {
                new[] { "What to measure", "Tracking setup", "Common attribution mistakes" },
                new[] { "Paid search", "Organic + content", "Email + lifecycle", "Quiz: channels" },
                new[] { "Designing reports", "Acting on data", "Capstone dashboard" },
            }
        ),
        (
            "Mobile App Design Patterns",
            "Design",
            "Modern mobile UX patterns for iOS and Android — navigation, lists, forms, and accessibility done right.",
            new[] { "Navigation", "Lists + Detail", "Inputs + Accessibility" },
            new[]
            {
                new[] { "Tabs vs stacks", "Modals vs full screen", "Deep linking" },
                new[] { "List density", "Detail layouts", "Empty states", "Quiz: lists" },
                new[] { "Input affordances", "Touch targets", "Accessibility essentials" },
            }
        ),
    };

    private static async Task<List<Course>> SeedCoursesAsync(
        ApplicationDbContext db,
        List<OrgSeed> orgs,
        SeededUsers users,
        Random rng,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var courses = new List<Course>();
        var coursesPerOrg = CourseLibrary.Length / orgs.Count; // 5 each

        var courseCursor = 0;
        foreach (var org in orgs)
        {
            var teachers = users.TeachersByOrg[org.Id];
            for (var i = 0; i < coursesPerOrg; i++)
            {
                var spec = CourseLibrary[courseCursor++];
                var teacher = teachers[i % teachers.Count];

                var course = new Course
                {
                    Id = Guid.NewGuid(),
                    TeacherId = teacher.Id,
                    OrganizationId = org.Id,
                    BranchId = teacher.BranchId,
                    Title = spec.Title,
                    Description = spec.Description,
                    Category = spec.Category,
                    MaxStudents = null,
                    IsPublished = i < 4, // last one per org is left as a draft for the screenshots
                    CreatedAt = now.AddDays(-30 + i * 3),
                    UpdatedAt = now.AddDays(-7 + i),
                };

                // Modules + lessons
                for (var m = 0; m < spec.Modules.Length; m++)
                {
                    var module = new Module
                    {
                        Id = Guid.NewGuid(),
                        CourseId = course.Id,
                        Title = spec.Modules[m],
                        Description = null,
                        Order = m + 1,
                        CreatedAt = course.CreatedAt,
                    };
                    course.Modules.Add(module);

                    var lessonTitles = spec.Lessons[m];
                    for (var l = 0; l < lessonTitles.Length; l++)
                    {
                        var title = lessonTitles[l];
                        var isQuiz = title.StartsWith("Quiz:", StringComparison.OrdinalIgnoreCase);
                        var isLab = title.StartsWith("Lab:", StringComparison.OrdinalIgnoreCase)
                                 || title.StartsWith("Capstone", StringComparison.OrdinalIgnoreCase)
                                 || title.StartsWith("Final", StringComparison.OrdinalIgnoreCase);
                        var type = isQuiz ? "Quiz" : isLab ? "Assignment" : (l % 3 == 0 ? "Text" : "Video");
                        var content = type switch
                        {
                            "Video" => JsonSerializer.Serialize(new
                            {
                                videoUrl = $"https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
                            }, JsonOpts),
                            "Text" => JsonSerializer.Serialize(new
                            {
                                body = $"# {title}\n\nThis lesson introduces the key ideas behind \"{title}\" and walks through a worked example. Read carefully — the quiz at the end of the module checks your understanding.",
                            }, JsonOpts),
                            _ => null,
                        };

                        var lesson = new Lesson
                        {
                            Id = Guid.NewGuid(),
                            ModuleId = module.Id,
                            Title = title,
                            Type = type,
                            Content = content,
                            Duration = type == "Video" ? 8 + (l % 4) * 4 : type == "Text" ? 5 : null,
                            Order = l + 1,
                            IsPublished = true,
                            CreatedAt = course.CreatedAt,
                            UpdatedAt = course.CreatedAt,
                        };
                        module.Lessons.Add(lesson);
                    }
                }

                // 1–2 assessments per course
                AddAssessment(course, "End-of-course assessment", "Quiz", passingScore: 70, isFinal: true);
                if (rng.NextDouble() < 0.6)
                {
                    AddAssessment(course, "Mid-course knowledge check", "Quiz", passingScore: 60, isFinal: false);
                }

                courses.Add(course);
                db.Courses.Add(course);
            }
        }

        await db.SaveChangesAsync(ct);

        // Announcements: one pinned welcome on each published course.
        var announcements = new List<Announcement>();
        foreach (var course in courses.Where(c => c.IsPublished))
        {
            announcements.Add(new Announcement
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                AuthorId = course.TeacherId,
                Title = "Welcome to the course",
                Body = $"Welcome! This course covers {course.Title.ToLowerInvariant()}. Work through the modules in order, take the end-of-course assessment, and reach out in the discussion area if you get stuck.",
                IsPinned = true,
                CreatedAt = course.CreatedAt.AddDays(1),
                UpdatedAt = course.CreatedAt.AddDays(1),
            });
        }
        db.Announcements.AddRange(announcements);
        await db.SaveChangesAsync(ct);

        return courses;
    }

    private static void AddAssessment(Course course, string title, string type, int passingScore, bool isFinal)
    {
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            CourseId = course.Id,
            Title = title,
            Type = type,
            TimeLimit = isFinal ? 30 : 15,
            PassingScore = passingScore,
            MaxAttempts = isFinal ? 2 : 3,
            CreatedAt = course.CreatedAt.AddDays(2),
        };

        // 5 generic-but-believable MCQ questions per assessment. These are
        // illustrative, not pedagogically tuned — the manual uses them to show
        // the quiz player UI, not to teach the subject.
        var bank = new (string Q, string[] Options, int Correct)[]
        {
            ("Which of the following best describes the goal of this course?",
                new[] { "Cover every topic in the field at survey depth", "Build a single foundational skill end-to-end", "Pass a certification exam", "Network with other learners" }, 1),
            ("What's the best approach when you get stuck on an exercise?",
                new[] { "Skip it and move on", "Re-read the lesson and try again", "Open a forum thread for help", "Both 2 and 3" }, 3),
            ("Which is the most important first step when starting a new project?",
                new[] { "Pick the trendiest framework", "Understand the problem you're solving", "Optimize for performance", "Write tests first" }, 1),
            ("Pair programming is most useful when…",
                new[] { "You want code to ship faster", "You're learning a new pattern with someone experienced", "You don't want to write tests", "Reviews are too slow" }, 1),
            ("Which describes a good feedback loop?",
                new[] { "Long and detailed", "Short, frequent, actionable", "Annual performance reviews", "Anonymous and one-way" }, 1),
        };

        for (var i = 0; i < bank.Length; i++)
        {
            var (text, options, correctIdx) = bank[i];
            assessment.Questions.Add(new Question
            {
                Id = Guid.NewGuid(),
                AssessmentId = assessment.Id,
                QuestionText = text,
                Type = "MCQ",
                Options = JsonSerializer.Serialize(options, JsonOpts),
                CorrectAnswer = correctIdx.ToString(),
                Points = 2,
                Order = i + 1,
            });
        }

        course.Assessments.Add(assessment);
    }

    // -------------------------------------------------------------------------
    // Enrollments + progress + certificates
    // -------------------------------------------------------------------------

    private static async Task SeedEnrollmentsAndProgressAsync(
        ApplicationDbContext db,
        List<Course> courses,
        SeededUsers users,
        Random rng,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var enrollments = new List<Enrollment>();
        var lessonProgress = new List<LessonProgress>();
        var certificates = new List<Certificate>();

        // Each student enrolls in 2–4 published courses from their own org.
        foreach (var (orgId, students) in users.StudentsByOrg)
        {
            var publishedOrgCourses = courses
                .Where(c => c.OrganizationId == orgId && c.IsPublished)
                .ToList();
            if (publishedOrgCourses.Count == 0) continue;

            foreach (var student in students)
            {
                var enrollCount = rng.Next(2, Math.Min(5, publishedOrgCourses.Count + 1));
                var picks = publishedOrgCourses
                    .OrderBy(_ => rng.Next())
                    .Take(enrollCount)
                    .ToList();

                foreach (var course in picks)
                {
                    // Progress buckets: 25% not-started, 35% mid, 25% advanced, 15% completed.
                    var roll = rng.NextDouble();
                    decimal pct;
                    bool completed;
                    if (roll < 0.25) { pct = 0; completed = false; }
                    else if (roll < 0.60) { pct = rng.Next(20, 50); completed = false; }
                    else if (roll < 0.85) { pct = rng.Next(60, 95); completed = false; }
                    else { pct = 100; completed = true; }

                    var enrolledAt = now.AddDays(-rng.Next(2, 60));
                    var enrollment = new Enrollment
                    {
                        Id = Guid.NewGuid(),
                        StudentId = student.Id,
                        CourseId = course.Id,
                        EnrolledAt = enrolledAt,
                        ProgressPercentage = pct,
                        IsCompleted = completed,
                        CompletedAt = completed ? enrolledAt.AddDays(rng.Next(5, 25)) : null,
                    };
                    enrollments.Add(enrollment);

                    // Mark lesson-level progress for the visible fraction so the
                    // dashboard course tiles look believable.
                    var lessons = course.Modules.SelectMany(m => m.Lessons).ToList();
                    var done = (int)Math.Round(lessons.Count * (double)(pct / 100m));
                    foreach (var lesson in lessons.Take(done))
                    {
                        lessonProgress.Add(new LessonProgress
                        {
                            Id = Guid.NewGuid(),
                            UserId = student.Id,
                            LessonId = lesson.Id,
                            CompletedAt = enrolledAt.AddDays(rng.Next(1, 20)),
                            WatchTimeSeconds = (lesson.Duration ?? 5) * 60,
                        });
                    }

                    if (completed)
                    {
                        // Verify code: short alphanumeric, matches the existing Certificate index.
                        var verify = $"LMS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}";
                        certificates.Add(new Certificate
                        {
                            Id = Guid.NewGuid(),
                            UserId = student.Id,
                            CourseId = course.Id,
                            IssuedAt = enrollment.CompletedAt!.Value,
                            VerifyCode = verify,
                        });
                    }
                }
            }
        }

        db.Enrollments.AddRange(enrollments);
        db.LessonProgress.AddRange(lessonProgress);
        db.Certificates.AddRange(certificates);
        await db.SaveChangesAsync(ct);
    }
}
