using System.Text;
using FluentValidation;
using MediatR;
using LMS.Application;
using LMS.Application.Auth;
using LMS.Application.Common;
using LMS.Infrastructure.Persistence;
using LMS.Infrastructure.Persistence.Seeding;
using LMS.Infrastructure.Services.Auth;
using LMS.WebAPI.Authorization;
using LMS.WebAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Serilog (read configuration from appsettings.json under "Serilog")
// ---------------------------------------------------------------------------
builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext());

// ---------------------------------------------------------------------------
// Persistence — SQL Server via EF Core
// Migrations live in LMS.Infrastructure.
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

// ---------------------------------------------------------------------------
// MediatR / AutoMapper / FluentValidation — all scan the Application assembly
// ---------------------------------------------------------------------------
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(AssemblyReference.Assembly));
builder.Services.AddAutoMapper(AssemblyReference.Assembly);
builder.Services.AddValidatorsFromAssembly(AssemblyReference.Assembly);

// FluentValidation runs automatically before every MediatR handler. Failures
// surface as ValidationException, which controllers translate to HTTP 400.
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(LMS.Application.Common.Behaviors.ValidationBehavior<,>));

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<INotificationService, LMS.Infrastructure.Services.Notifications.NotificationService>();
builder.Services.Configure<LMS.Infrastructure.Services.Email.SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, LMS.Infrastructure.Services.Email.SmtpEmailSender>();
builder.Services.AddScoped<IAccountLifecycleService, LMS.Infrastructure.Services.Auth.AccountLifecycleService>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

// Expose the EF Core ApplicationDbContext to MediatR handlers via the Application
// abstraction, so Application code never references the concrete type.
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// Current-user accessor for handlers that need the authenticated caller's id/role.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ---------------------------------------------------------------------------
// Authentication — JWT Bearer
// ---------------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Permission-based authorization. Endpoints opt-in via [RequirePermission("...")];
// the custom policy provider materializes the policy on demand and the handler
// matches the user's `perm` claims against the requirement.
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ---------------------------------------------------------------------------
// CORS — allow the Angular dev server
// ---------------------------------------------------------------------------
const string CorsPolicyName = "AllowAngular";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ---------------------------------------------------------------------------
// Web API
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Apply EF Core migrations automatically in Development. Lets `docker compose
// up` bring up a fresh SQL Server volume and have the schema ready without a
// manual `dotnet ef database update` step. Idempotent for existing databases.
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Tenancy bootstrap: seeds Permissions + RolePermissions, ensures the Default
    // Organization/Branch exist, renames legacy Admin -> SuperAdmin, and backfills
    // users that landed without a branch. Idempotent.
    var seedLogger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("TenancySeeder");
    await TenancySeeder.SeedAsync(db, seedLogger);

    // Sample data: only runs when the catalog is empty (i.e. on a freshly
    // dropped dev DB). Produces 3 orgs / ~80 users / 15 courses with lessons,
    // assessments, enrollments and a few completions so the UI looks real.
    var sampleLogger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("SampleDataSeeder");
    await SampleDataSeeder.SeedAsync(db, sampleLogger);
}

// ---------------------------------------------------------------------------
// HTTP pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

// Serve uploaded files (lesson documents) from wwwroot as static content.
app.UseStaticFiles();

// Skip HTTPS redirect in Development so the Angular dev server can call the HTTP
// listener directly. A 307 from http→https on the API origin breaks browser CORS
// preflight for cross-scheme redirects.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Smoke endpoint to verify the API boots before controllers exist.
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

app.Run();
