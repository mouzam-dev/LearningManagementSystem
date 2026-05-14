using LMS.Application.Common;

namespace LMS.Infrastructure.Services.Auth;

/// <summary>
/// BCrypt-backed <see cref="IPasswordHasher"/>. Work factor 12 matches the
/// cost used by <see cref="AuthService"/> for register/login, so hashes are
/// interchangeable regardless of which path created them.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
