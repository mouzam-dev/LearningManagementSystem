namespace LMS.Application.Auth.Dtos;

public class GoogleLoginRequest
{
    /// <summary>
    /// The ID token (a signed JWT) issued by Google Identity Services on the
    /// client after the user picks an account. The server validates it against
    /// Google's public keys and the configured OAuth client id.
    /// </summary>
    public string IdToken { get; set; } = string.Empty;
}
