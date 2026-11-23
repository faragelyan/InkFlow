namespace InkFlow.Infrastructure.ExternalServices.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "InkFlow";
    public string FromAddress { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string ClientBaseUrl { get; set; } = "http://localhost:5035";
}
