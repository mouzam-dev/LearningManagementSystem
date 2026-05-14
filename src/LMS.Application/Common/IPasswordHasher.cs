namespace LMS.Application.Common;

/// <summary>
/// Password hashing abstraction so Application-layer handlers (e.g. the
/// change-password command) can hash + verify without taking a direct
/// dependency on the BCrypt library, which lives in Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password for storage.</summary>
    string Hash(string password);

    /// <summary>Verifies a plaintext password against a stored hash.</summary>
    bool Verify(string password, string hash);
}
