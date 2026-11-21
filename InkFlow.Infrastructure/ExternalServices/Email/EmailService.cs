using Microsoft.Extensions.Logging;
using InkFlow.Application.Interfaces.ExternalServices;

namespace InkFlow.Infrastructure.ExternalServices.Email;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending email to {To} with Subject: {Subject}", to, subject);
        return Task.CompletedTask;
    }

    public Task SendWelcomeEmailAsync(string to, string displayName, CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to InkFlow!";
        var body = $"<h1>Hello {displayName},</h1><p>Welcome to InkFlow. We are thrilled to have you here.</p>";
        return SendEmailAsync(to, subject, body, cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default)
    {
        var subject = "Reset your InkFlow Password";
        var body = $"<p>Click <a href=\"{resetLink}\">here</a> to reset your password.</p>";
        return SendEmailAsync(to, subject, body, cancellationToken);
    }
}
