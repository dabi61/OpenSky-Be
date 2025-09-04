using System.Net;
using System.Net.Mail;

namespace BE_OPENSKY.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly bool _enableSsl;
    private readonly string _senderEmail;
    private readonly string _senderName;
    private readonly string _username;
    private readonly string _password;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // SMTP config - SendGrid SMTP Relay for Railway, Gmail for local
        var sendGridSmtpPassword = Environment.GetEnvironmentVariable("SENDGRID_SMTP_PASSWORD");
        
        if (!string.IsNullOrEmpty(sendGridSmtpPassword))
        {
            // Use SendGrid SMTP Relay for Railway
            _smtpHost = "smtp.sendgrid.net";
            _smtpPort = 465; 
            _enableSsl = true;
            _username = "apikey";
            _password = sendGridSmtpPassword;
        }
        else
        {
            // Use Gmail SMTP for local development
            _smtpHost = "smtp.gmail.com";
            _smtpPort = 587;
            _enableSsl = true;
            _username = "cuongngba7@gmail.com";
            _password = "ccmn nrsx qzwb bmmq";
        }
        
        _senderEmail = _configuration["Email:SenderEmail"] ?? "cuongngba7@gmail.com";
        _senderName = _configuration["Email:SenderName"] ?? "OpenSky Travel";
        
        _logger.LogInformation("EmailService initialized with SMTP: {Host}:{Port}, Sender: {Email}", _smtpHost, _smtpPort, _senderEmail);
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var subject = "Reset Mật Khẩu - OpenSky Travel";
        var htmlBody = GeneratePasswordResetEmailTemplate(resetToken);
        
        return await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            using var client = new SmtpClient(_smtpHost, _smtpPort);
            client.EnableSsl = _enableSsl;
            client.Credentials = new NetworkCredential(_username, _password);

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_senderEmail, _senderName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.Body = htmlBody;
            mailMessage.IsBodyHtml = true;

            await client.SendMailAsync(mailMessage);
            
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    private string GeneratePasswordResetEmailTemplate(string resetToken)
    {
        // Tạo link reset password - Railway domain
        var resetLink = $"https://opensky-be-production.up.railway.app/reset-password?token={resetToken}";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Reset Mật Khẩu</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>OpenSky Travel</h1>
        </div>
        <div class='content'>
            <h2>Yêu cầu Reset Mật Khẩu</h2>
            <p>Chúng tôi nhận được yêu cầu reset mật khẩu cho tài khoản của bạn.</p>
            <p>Nhấp vào nút bên dưới để reset mật khẩu:</p>
            <p style='text-align: center; margin: 30px 0;'>
                <a href='{resetLink}' class='button'>Reset Mật Khẩu</a>
            </p>
            <p><strong>Mã Token:</strong> {resetToken}</p>
            <p><strong>Lưu ý:</strong> Link này sẽ hết hạn sau 30 phút.</p>
            <p>Nếu bạn không yêu cầu reset mật khẩu, vui lòng bỏ qua email này.</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 OpenSky Travel. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
