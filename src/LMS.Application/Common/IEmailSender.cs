namespace LMS.Application.Common;

/// <summary>
/// Email abstraction. The current dev implementation logs to Serilog instead
/// of hitting an SMTP server — this slice keeps email out of the critical
/// path until SMTP credentials are wired up in a follow-up. Callers can rely
/// on the contract today; the transport will swap underneath.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}
