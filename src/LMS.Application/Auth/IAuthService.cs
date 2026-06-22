using LMS.Application.Auth.Dtos;

namespace LMS.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Signs a user in with a Google ID token. Validates the token, then links to
    /// the existing account with that email or creates a new Student account.
    /// </summary>
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default);
}
