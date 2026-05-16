using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using LMS.Application.Auth;
using LMS.Application.Auth.Dtos;
using LMS.Domain.Entities;
using LMS.Infrastructure.Persistence;
using LMS.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LMS.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    // JWT custom claim type names. Duplicated from LmsClaims (which lives in WebAPI)
    // because Infrastructure can't reference WebAPI. Keep in sync.
    private const string ClaimOrgId = "org_id";
    private const string ClaimBranchId = "branch_id";
    private const string ClaimPermission = "perm";

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IMapper _mapper;
    private readonly IAccountLifecycleService _lifecycle;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IConfiguration config,
        IMapper mapper,
        IAccountLifecycleService lifecycle,
        ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _mapper = mapper;
        _lifecycle = lifecycle;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var emailExists = await _db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == request.Email, ct);

        if (emailExists)
        {
            return new AuthResponse { Success = false, Message = "Email already registered." };
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            IsVerified = false,
            IsActive = true,
            // Public sign-ups (Student/Teacher) land in the Default tenant. An
            // OrgAdmin can transfer them later. Without this they'd be orphans.
            OrganizationId = TenancySeeder.DefaultOrganizationId,
            BranchId = TenancySeeder.DefaultBranchId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Fire-and-forget the verification email — failure here shouldn't
        // block registration; the user can request a fresh link later.
        try
        {
            await _lifecycle.IssueEmailVerificationAsync(user.Id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send verification email to {Email}", user.Email);
        }

        var permissions = await LoadPermissionsAsync(user.Role, ct);
        var (access, refresh) = GenerateTokens(user, permissions);

        return new AuthResponse
        {
            Success = true,
            Message = "Registration successful. Check your email to verify your account.",
            AccessToken = access,
            RefreshToken = refresh,
            User = _mapper.Map<UserDto>(user)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse { Success = false, Message = "Invalid credentials." };
        }

        if (!user.IsActive)
        {
            return new AuthResponse { Success = false, Message = "Account is inactive." };
        }

        var permissions = await LoadPermissionsAsync(user.Role, ct);
        var (access, refresh) = GenerateTokens(user, permissions);

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful.",
            AccessToken = access,
            RefreshToken = refresh,
            User = _mapper.Map<UserDto>(user)
        };
    }

    private async Task<List<string>> LoadPermissionsAsync(string role, CancellationToken ct)
    {
        return await _db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.Role == role)
            .Select(rp => rp.Permission.Code)
            .ToListAsync(ct);
    }

    private (string AccessToken, string RefreshToken) GenerateTokens(User user, IEnumerable<string> permissions)
    {
        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
        var issuer = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var expirationMinutes = int.TryParse(_config["Jwt:ExpirationMinutes"], out var m) ? m : 60;
        var refreshDays = int.TryParse(_config["Jwt:RefreshTokenDays"], out var d) ? d : 7;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };

        if (user.OrganizationId.HasValue)
        {
            claims.Add(new Claim(ClaimOrgId, user.OrganizationId.Value.ToString()));
        }
        if (user.BranchId.HasValue)
        {
            claims.Add(new Claim(ClaimBranchId, user.BranchId.Value.ToString()));
        }
        foreach (var code in permissions)
        {
            claims.Add(new Claim(ClaimPermission, code));
        }

        var access = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds);

        var refresh = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
            expires: DateTime.UtcNow.AddDays(refreshDays),
            signingCredentials: creds);

        var handler = new JwtSecurityTokenHandler();
        return (handler.WriteToken(access), handler.WriteToken(refresh));
    }
}
