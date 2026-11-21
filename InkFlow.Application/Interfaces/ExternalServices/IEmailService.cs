namespace InkFlow.Application.Interfaces.ExternalServices;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string to, string displayName, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string to, string resetLink, CancellationToken cancellationToken = default);
}
