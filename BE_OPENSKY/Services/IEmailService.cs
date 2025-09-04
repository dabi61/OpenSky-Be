namespace BE_OPENSKY.Services;

public interface IEmailService
{
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken);
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
}
