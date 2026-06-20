using System.Text;
using FluentValidation;
using MediatR;
using LMS.Application;
using LMS.Application.Auth;
using LMS.Application.Common;
using LMS.Application.Student.Services;
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

// QuestPDF needs its Community license registered before the first PDF is rendered.
// Free under their non-commercial / under-$1M-revenue terms — fine for this LMS demo.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddSingleton<ICertificatePdfService,
    LMS.Infrastructure.Services.Certificates.CertificatePdfService>();

// Sunnah hadith. Data lives in the local Hadiths/HadithCollections tables, rebuilt
// from the source APIs by HadithHarvestService (SuperAdmin "Refresh"); reads are
// served by DbSunnahService. Collection/book lists are cached in-memory.
builder.Services.AddMemoryCache();
builder.Services.AddScoped<LMS.WebAPI.Services.Sunnah.ISunnahService, LMS.WebAPI.Services.Sunnah.DbSunnahService>();

// Harvest (rebuild from source): background job + status, plus the two upstream
// clients — Sunnah.com (keyed) for most collections, fawazahmed0's CDN for Malik.
builder.Services.AddSingleton<LMS.WebAPI.Services.Sunnah.HadithHarvestStatus>();
builder.Services.AddSingleton<LMS.WebAPI.Services.Sunnah.HadithHarvestService>();
builder.Services.AddHttpClient("sunnah", c =>
{
    var baseUrl = builder.Configuration["Sunnah:BaseUrl"] ?? "https://api.sunnah.com/v1/";
    if (!baseUrl.EndsWith('/')) baseUrl += "/";
    c.BaseAddress = new Uri(baseUrl);
    var key = builder.Configuration["Sunnah:ApiKey"];
    if (!string.IsNullOrWhiteSpace(key)) c.DefaultRequestHeaders.Add("X-API-Key", key);
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("fawaz", c =>
{
    c.BaseAddress = new Uri("https://cdn.jsdelivr.net/gh/fawazahmed0/hadith-api@1/");
    c.Timeout = TimeSpan.FromSeconds(90);
});

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
    {
        // Skip configuring origins entirely when none are listed — happens in
        // production where the SPA is served from the same App Service as the
        // API and no cross-origin requests exist. Calling WithOrigins([]) is
        // a misconfiguration ASP.NET refuses to start with.
        if (allowedOrigins.Length == 0) return;
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    }));

// ---------------------------------------------------------------------------
// Web API
// ---------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.MapType<IFormFile>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "binary" });
    c.CustomSchemaIds(type => type.FullName);
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Apply EF Core migrations automatically. Always runs (Development + Production)
// so the first deploy to Azure picks up the schema without a manual step.
// Idempotent for existing databases. The sample-data seeder is itself idempotent
// (it bails out the moment one Course exists) so re-running on each cold start
// is safe — only fires on a fresh database.
//
// Disable with environment variable LMS_SKIP_AUTO_MIGRATE=true if you ever need
// to deploy without touching the DB schema.
// ---------------------------------------------------------------------------
var skipAutoMigrate = string.Equals(
    Environment.GetEnvironmentVariable("LMS_SKIP_AUTO_MIGRATE"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (!skipAutoMigrate)
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

    // Sample data: only runs when the catalog is empty. Produces 3 orgs / ~80
    // users / 15 courses with lessons, assessments, enrollments and a few
    // completions so the UI looks real. Set LMS_SEED_SAMPLE=false in App Service
    // settings if you want an empty production DB.
    var seedSample = !string.Equals(
        Environment.GetEnvironmentVariable("LMS_SEED_SAMPLE"),
        "false",
        StringComparison.OrdinalIgnoreCase);
    if (seedSample)
    {
        var sampleLogger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SampleDataSeeder");
        await SampleDataSeeder.SeedAsync(db, sampleLogger);
    }
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

// ---------------------------------------------------------------------------
// SPA fallback — when the Angular build has been copied into wwwroot during a
// production deploy, any non-API request that doesn't match a static file
// should return index.html so the Angular Router can take over. Lets a single
// App Service host both the API and the SPA (no CORS, no second origin).
//
// In Development we don't enable this so /api routes still 404 cleanly when
// the Angular dev server isn't proxying.
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    var indexPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "index.html");
    if (File.Exists(indexPath))
    {
        app.MapFallback(async ctx =>
        {
            // Don't swallow API misses — let them 404 normally.
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.SendFileAsync(indexPath);
        });
    }
}

app.Run();
