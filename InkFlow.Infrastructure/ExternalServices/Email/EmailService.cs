using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InkFlow.Application.Interfaces.ExternalServices;

namespace InkFlow.Infrastructure.ExternalServices.Email;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailOptions _options;

    public EmailService(ILogger<EmailService> logger, IOptions<EmailOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpHost) ||
            string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            _logger.LogWarning("Email was not sent because SMTP host or from address is not configured.");
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_options.Username, _options.Password)
            };

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Sent email to {To} with subject {Subject}", to, subject);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send email to {To}", to);
        }
    }

    public Task SendWelcomeEmailAsync(string to, string displayName, CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to InkFlow!";
        var body = $"<h1>Hello {displayName},</h1><p>Welcome to InkFlow. We are thrilled to have you here.</p>";
        return SendEmailAsync(to, subject, body, cancellationToken);
    }

    public Task SendEmailConfirmationEmailAsync(string to, string confirmationLink, CancellationToken cancellationToken = default)
    {
        var subject = "Confirm your InkFlow Email";
        var absoluteLink = BuildAbsoluteLink(confirmationLink);
        var body = $"<p>Click <a href=\"{absoluteLink}\">here</a> to confirm your email address.</p>";
        return SendEmailAsync(to, subject, body, cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default)
    {
        var subject = "Reset your InkFlow Password";
        var absoluteLink = BuildAbsoluteLink(resetLink);
        var body = $"<p>Click <a href=\"{absoluteLink}\">here</a> to reset your password.</p>";
        return SendEmailAsync(to, subject, body, cancellationToken);
    }

    private string BuildAbsoluteLink(string link)
    {
        return Uri.TryCreate(link, UriKind.Absolute, out var absolute)
            ? absolute.ToString()
            : $"{_options.ClientBaseUrl.TrimEnd('/')}/{link.TrimStart('/')}";
    }
}
