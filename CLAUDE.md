# LMS — Learning Management System

A full-stack LMS for Students, Teachers, and Admins. 58 features across 3 modules. Target: 8-week build to production.

## Source of Truth

The authoritative project documents live in [.claude/](.claude/). Read these before suggesting features or architecture changes:

- **[.claude/LMS_BRD_DotNetCore_Angular.docx](.claude/LMS_BRD_DotNetCore_Angular.docx)** — Feature specifications (what to build). 58 features, API endpoints, acceptance criteria, NFRs.
- **[.claude/LMS_DotNet_Angular_Development_Guide.md](.claude/LMS_DotNet_Angular_Development_Guide.md)** — Week-by-week implementation roadmap with code examples.
- **[.claude/LMS_DotNet_Angular_Quick_Reference.md](.claude/LMS_DotNet_Angular_Quick_Reference.md)** — C# / Angular code patterns, checklists, common errors.
- **[.claude/GETTING_STARTED_DotNet_Angular.md](.claude/GETTING_STARTED_DotNet_Angular.md)** — Setup overview and 30-min quick start.
- **[.claude/COMPLETE_PACKAGE_SUMMARY.md](.claude/COMPLETE_PACKAGE_SUMMARY.md)** — Index of the package.

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime (backend) | **.NET 8 LTS** (TargetFramework `net8.0`) — chosen for VS 2022 17.8 compatibility |
| Web framework | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8.0.x |
| Database | SQL Server 2022 (LocalDB for dev: `MSSQLLocalDB`) |
| Auth | JWT (BCrypt for password hashing) |
| Validation | FluentValidation 11.x |
| Mapping | AutoMapper 12.x |
| App pattern | CQRS via MediatR 12.x |
| Logging | Serilog |
| API docs | Swashbuckle (Swagger UI at `/swagger`) |
| Testing (backend) | xUnit + Moq |
| Frontend framework | Angular 20 (docs target 17+ — newer Signals / `@if`/`@for` available) |
| Frontend language | TypeScript (strict) |
| State | Signals + RxJS |
| Styling | Tailwind CSS (Angular Material optional) |
| Testing (frontend) | Jasmine + Karma; Cypress for E2E |

> **Why .NET 8 and not .NET 10?** The user's Visual Studio install ships with the .NET 8 SDK (8.0.400), and older VS 2022 versions don't recognize `net10.0`. `net8.0` builds and runs identically on both VS and CLI without needing a VS update. To migrate to .NET 10 later: update VS to 17.12+, then change `<TargetFramework>net8.0</TargetFramework>` → `net10.0` in all 5 csproj and bump EF Core / ASP.NET Core packages to 10.0.x.

Docker is optional for local dev. SQL Server LocalDB is installed and works without containers.

## Architecture

### Backend — Clean / Layered

```
src/
├── LMS.Domain/          # Entities, domain interfaces. No dependencies on other layers.
├── LMS.Application/     # DTOs, MediatR handlers, services, validators. Depends on Domain.
├── LMS.Infrastructure/  # DbContext, repositories, EF migrations, external integrations. Depends on Domain.
└── LMS.WebAPI/          # Controllers, middleware, Program.cs DI wiring. Depends on Application.
tests/
└── LMS.Tests/           # xUnit + Moq.
```

Reference rule: **Domain has no references; Application references Domain; Infrastructure references Application + Domain; WebAPI references Application + Infrastructure.** Application owns abstractions (interfaces, DTOs); Infrastructure implements them (DbContext, AuthService, repositories). WebAPI is the composition root that wires concretes into DI.

### Frontend — Feature-based

```
lms-angular/src/app/
├── core/      # Global services, guards, interceptors (auth, error, JWT)
├── shared/    # Shared components, pipes, directives
├── auth/      # Login, register
├── student/   # 22 features (dashboard, catalog, video player, quizzes, certificates)
├── teacher/   # 21 features (course builder, grading, analytics)
└── admin/     # 15 features (user mgmt, moderation, settings, reporting)
```

Default to **standalone components with Signals**. Use Reactive Forms (typed). Don't use NgModules unless integrating a third-party module that requires one.

## Commands

### Backend
```bash
dotnet restore
dotnet build
dotnet run --project src/LMS.WebAPI            # http://localhost:5000
dotnet test
# EF migrations (run from repo root)
dotnet ef migrations add <Name> -p src/LMS.Infrastructure -s src/LMS.WebAPI
dotnet ef database update     -p src/LMS.Infrastructure -s src/LMS.WebAPI
```

### Frontend
```bash
cd lms-angular
npm install
ng serve                                        # http://localhost:4200
ng test
ng build --configuration production
```

## Conventions

- **Async/await everywhere** in C#; pass `CancellationToken` through handlers.
- **`AsNoTracking()` for read-only EF queries.** Always paginate list endpoints.
- **DTOs at the boundary** — never return EF entities from controllers.
- **MediatR `IRequest<T>` per use case** — one handler per command/query, kept thin.
- **FluentValidation for every request DTO.**
- **JWT in `Authorization: Bearer` header**, validated by `JwtBearer` middleware.
- **CORS policy** scoped to `http://localhost:4200` in dev.
- **Angular: `inject()` over constructor injection** for new components.
- **Angular: `signal()` for component state**, RxJS only for streams (HTTP, events).
- **Angular: `trackBy` on every `*ngFor` / track on every `@for`.**
- Commit messages: conventional style (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`).

## 8-Week Timeline

| Week | Phase | Output |
|---|---|---|
| 1 | Foundation: project setup, EF schema, JWT auth, Docker (optional) | Working auth on localhost:5000 + 4200 |
| 2–3 | Student module (22 features) | Complete student experience |
| 4–5 | Teacher module (21 features) | Complete teacher experience |
| 6 | Admin module (15 features) | Complete admin capabilities |
| 7 | Integration, perf, security hardening | Production-ready |
| 8 | UAT, docs, deploy, monitoring | Live |

## Where to Look

| Question | File |
|---|---|
| What should this feature do? | `.claude/LMS_BRD_DotNetCore_Angular.docx` |
| How do I implement X in C#? | `.claude/LMS_DotNet_Angular_Quick_Reference.md` (C# patterns) |
| How do I implement X in Angular? | `.claude/LMS_DotNet_Angular_Quick_Reference.md` (Angular patterns) |
| What's the schedule for today? | `.claude/LMS_DotNet_Angular_Development_Guide.md` (week/day) |
| I have an error | `.claude/LMS_DotNet_Angular_Quick_Reference.md` (Common Errors) |
| Deploy steps | `.claude/LMS_DotNet_Angular_Quick_Reference.md` (Deployment checklist) |
