using System.Text;
using FluentValidation;
using LMS.Application;
using LMS.Application.Auth;
using LMS.Infrastructure.Persistence;
using LMS.Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();

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

builder.Services.AddAuthorization();

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
