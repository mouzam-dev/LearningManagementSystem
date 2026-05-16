using LMS.Application.Common;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services.Email;

/// <summary>
/// Dev-only email "sender" that just logs. Keeps the contract honest while
/// SMTP credentials and a real transport are wired up in a follow-up slice.
/// Production swap target: a Serilog-friendly MailKit implementation.
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[email-dev] to={To} subject={Subject} bodyLength={Length}",
            toEmail, subject, body?.Length ?? 0);
        return Task.CompletedTask;
    }
}
