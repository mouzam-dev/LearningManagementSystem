using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Google.Apis.Auth;
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

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        var clientId = _config["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return new AuthResponse { Success = false, Message = "Google sign-in is not configured on the server." };
        }
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return new AuthResponse { Success = false, Message = "Missing Google credential." };
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            // Validates signature against Google's public keys + checks the token's
            // audience is our OAuth client id, the issuer is Google, and it hasn't expired.
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Rejected Google sign-in: invalid token.");
            return new AuthResponse { Success = false, Message = "Google sign-in failed: the token was invalid or has expired." };
        }

        if (string.IsNullOrWhiteSpace(payload.Email) || payload.EmailVerified != true)
        {
            return new AuthResponse { Success = false, Message = "Your Google account does not have a verified email." };
        }

        var email = payload.Email.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        var now = DateTime.UtcNow;
        var isNew = false;

        if (user is null)
        {
            isNew = true;
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = FirstNonEmpty(payload.GivenName, FirstWord(payload.Name), LocalPart(email)),
                LastName = FirstNonEmpty(payload.FamilyName, RestWords(payload.Name)),
                Role = "Student",
                // Google users authenticate via Google; store an unusable random local
                // password so the column is non-null. They can set one via "forgot password".
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), workFactor: 12),
                ProfilePictureUrl = string.IsNullOrWhiteSpace(payload.Picture) ? null : payload.Picture,
                IsVerified = true, // Google has already verified the email
                IsActive = true,
                // Public sign-ups land in the Default tenant (same as RegisterAsync).
                OrganizationId = TenancySeeder.DefaultOrganizationId,
                BranchId = TenancySeeder.DefaultBranchId,
                CreatedAt = now,
                UpdatedAt = now,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            if (!user.IsActive)
            {
                return new AuthResponse { Success = false, Message = "Account is inactive." };
            }
            // Linking Google to an existing email/password account: mark verified and
            // backfill the avatar if we don't have one yet.
            var changed = false;
            if (!user.IsVerified) { user.IsVerified = true; changed = true; }
            if (string.IsNullOrWhiteSpace(user.ProfilePictureUrl) && !string.IsNullOrWhiteSpace(payload.Picture))
            {
                user.ProfilePictureUrl = payload.Picture;
                changed = true;
            }
            if (changed)
            {
                user.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
            }
        }

        var permissions = await LoadPermissionsAsync(user.Role, ct);
        var (access, refresh) = GenerateTokens(user, permissions);

        return new AuthResponse
        {
            Success = true,
            Message = isNew ? "Welcome! Your account was created with Google." : "Login successful.",
            AccessToken = access,
            RefreshToken = refresh,
            User = _mapper.Map<UserDto>(user)
        };
    }

    // -- Google profile name helpers ----------------------------------------
    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;

    private static string LocalPart(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }

    private static string? FirstWord(string? name)
        => string.IsNullOrWhiteSpace(name) ? null : name.Trim().Split(' ', 2)[0];

    private static string? RestWords(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var parts = name.Trim().Split(' ', 2);
        return parts.Length > 1 ? parts[1] : null;
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
