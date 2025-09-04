using SendGrid;
using SendGrid.Helpers.Mail;

namespace BE_OPENSKY.Services;

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;
    
    private readonly string _senderEmail;
    private readonly string _senderName;

    public SendGridEmailService(ISendGridClient sendGridClient, IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
        _sendGridClient = sendGridClient;
        _configuration = configuration;
        _logger = logger;
        
        // Railway compatibility - Environment variables first, then config
        _senderEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL") 
            ?? _configuration["Email:SenderEmail"] 
            ?? "noreply@opensky.com";
        _senderName = Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME") 
            ?? _configuration["Email:SenderName"] 
            ?? "OpenSky Travel";
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
            var from = new EmailAddress(_senderEmail, _senderName);
            var to = new EmailAddress(toEmail);
            
            var msg = MailHelper.CreateSingleEmail(from, to, subject, htmlBody, htmlBody);
            
            var response = await _sendGridClient.SendEmailAsync(msg);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully via SendGrid to {Email}", toEmail);
                return true;
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid email failed with status {Status}: {Body}", response.StatusCode, body);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SendGrid to {Email}: {Message}", toEmail, ex.Message);
            return false;
        }
    }

    private string GeneratePasswordResetEmailTemplate(string resetToken)
    {
        // Sử dụng Railway domain thay vì localhost
        var resetLink = $"https://opensky-be-production.up.railway.app/reset-password?token={resetToken}";
        
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Mật Khẩu</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ padding: 30px; background-color: #f9f9f9; }}
        .button {{ 
            display: inline-block; 
            padding: 15px 30px; 
            background-color: #007bff; 
            color: white; 
            text-decoration: none; 
            border-radius: 5px; 
            font-weight: bold;
        }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 14px; }}
        .token-box {{ 
            background: #e9ecef; 
            padding: 15px; 
            border-radius: 5px; 
            margin: 20px 0; 
            font-family: monospace; 
            word-break: break-all;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🌤️ OpenSky Travel</h1>
            <h2>Reset Mật Khẩu</h2>
        </div>
        <div class='content'>
            <p>Xin chào,</p>
            <p>Chúng tôi nhận được yêu cầu reset mật khẩu cho tài khoản của bạn.</p>
            <p>Nhấp vào nút bên dưới để reset mật khẩu:</p>
            
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{resetLink}' class='button'>Reset Mật Khẩu</a>
            </div>
            
            <p><strong>Hoặc sử dụng mã token này:</strong></p>
            <div class='token-box'>
                {resetToken}
            </div>
            
            <p><strong>Lưu ý quan trọng:</strong></p>
            <ul>
                <li>Link này sẽ hết hạn sau <strong>30 phút</strong></li>
                <li>Chỉ sử dụng được <strong>một lần</strong></li>
                <li>Nếu bạn không yêu cầu reset mật khẩu, vui lòng bỏ qua email này</li>
            </ul>
        </div>
        <div class='footer'>
            <p>&copy; 2025 OpenSky Travel. All rights reserved.</p>
            <p>Email này được gửi tự động, vui lòng không reply.</p>
        </div>
    </div>
</body>
</html>";
    }
}
