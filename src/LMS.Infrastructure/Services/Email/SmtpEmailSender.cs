using System.Net;
using System.Net.Mail;
using LMS.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMS.Infrastructure.Services.Email;

/// <summary>
/// Sends mail via SMTP using the BCL <see cref="SmtpClient"/>. If SMTP isn't
/// configured (no host in <c>appsettings.Smtp</c>) we log the would-be email
/// instead of throwing — keeps the dev loop fast and CI green without a
/// mailserver, while making intent clear in the logs.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            // Dev fallback — make the email visible without delivering it.
            _logger.LogInformation(
                "[email-dev] to={To} subject={Subject} body=\n{Body}",
                toEmail, subject, body);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_options.Host!, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);
        }

        try
        {
            await client.SendMailAsync(message, ct);
            _logger.LogInformation(
                "[email-sent] to={To} subject={Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            // SMTP failure shouldn't break the user-facing request (e.g. a
            // failed reset email would otherwise leave the user with a 500
            // and a partially-completed flow). Log + swallow; the caller
            // returns success and we surface the issue via observability.
            _logger.LogError(ex,
                "[email-failed] to={To} subject={Subject}", toEmail, subject);
        }
    }
}
