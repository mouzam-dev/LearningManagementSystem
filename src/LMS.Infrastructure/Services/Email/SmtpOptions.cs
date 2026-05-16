namespace LMS.Infrastructure.Services.Email;

/// <summary>
/// SMTP settings read from <c>appsettings.json -> Smtp</c>.
/// When <see cref="Host"/> is null / empty the email sender falls back to
/// logging instead of attempting a network send, so local dev works without
/// any SMTP server. Production should set Host + Port + From at minimum.
/// </summary>
public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 25;
    public bool EnableSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@lms.local";
    public string FromName { get; set; } = "LMS";

    /// <summary>Convenience flag: are we configured for real delivery?</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
