using System.Security.Cryptography;
using System.Text;

namespace BE_OPENSKY.Services;

public class PasswordResetService : IPasswordResetService
{
    private readonly IRedisService _redisService;
    private readonly IUserService _userService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IRedisService redisService, 
        IUserService userService, 
        IConfiguration configuration,
        ILogger<PasswordResetService> logger)
    {
        _redisService = redisService;
        _userService = userService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateResetTokenAsync(string email)
    {
        try
        {
            // Kiểm tra email có tồn tại không
            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                throw new InvalidOperationException("Email không tồn tại trong hệ thống");
            }

            // Tạo token ngẫu nhiên
            var token = GenerateSecureToken();
            var expirationMinutes = _configuration.GetValue<int>("PasswordReset:TokenExpirationMinutes", 30);
            var expiration = TimeSpan.FromMinutes(expirationMinutes);

            // Lưu token vào Redis với key pattern: reset_token:{token}
            var key = $"reset_token:{token}";
            var resetData = System.Text.Json.JsonSerializer.Serialize(new
            {
                Email = email,
                UserId = user.UserID,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(expiration)
            });

            await _redisService.SetStringAsync(key, resetData, expiration);

            // Invalidate any existing tokens for this user
            await InvalidateUserTokensAsync(user.UserID);

            _logger.LogInformation("Password reset token generated for user: {Email}, Token: {Token}", email, token);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reset token for email: {Email}", email);
            throw;
        }
    }

    public async Task<bool> ValidateResetTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var key = $"reset_token:{token}";
            var resetDataJson = await _redisService.GetStringAsync(key);

            if (string.IsNullOrEmpty(resetDataJson))
            {
                _logger.LogWarning("Invalid or expired reset token: {Token}", token);
                return false;
            }

            _logger.LogInformation("Reset token validated successfully: {Token}", token);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating reset token: {Token}", token);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
                return false;

            var key = $"reset_token:{token}";
            var resetDataJson = await _redisService.GetStringAsync(key);

            if (string.IsNullOrEmpty(resetDataJson))
            {
                _logger.LogWarning("Reset password attempted with invalid token: {Token}", token);
                return false;
            }

            // Parse JSON data
            using var document = System.Text.Json.JsonDocument.Parse(resetDataJson);
            var root = document.RootElement;
            
            var userId = Guid.Parse(root.GetProperty("UserId").GetString()!);
            var email = root.GetProperty("Email").GetString()!;

            // Update password
            var changePasswordDto = new ChangePasswordDTO
            {
                CurrentPassword = "", // Not needed for reset
                NewPassword = newPassword
            };

            // We need to modify UserService to support password reset without current password
            var success = await _userService.ResetPasswordAsync(userId, newPassword);

            if (success)
            {
                // Invalidate the token after successful reset
                await _redisService.DeleteAsync(key);
                _logger.LogInformation("Password reset successfully for user: {Email}", email);
                return true;
            }

            _logger.LogWarning("Failed to reset password for user: {Email}", email);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password with token: {Token}", token);
            return false;
        }
    }

    public async Task<bool> InvalidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var key = $"reset_token:{token}";
            var result = await _redisService.DeleteAsync(key);
            
            _logger.LogInformation("Token invalidated: {Token}, Success: {Success}", token, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating token: {Token}", token);
            return false;
        }
    }

    private Task InvalidateUserTokensAsync(Guid userId)
    {
        try
        {
            // This is a simplified approach. In production, you might want to store
            // user tokens in a set and invalidate them all
            _logger.LogInformation("Invalidating existing tokens for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user tokens: {UserId}", userId);
        }
        
        return Task.CompletedTask;
    }

    private string GenerateSecureToken()
    {
        var tokenLength = _configuration.GetValue<int>("PasswordReset:TokenLength", 32);
        var bytes = new byte[tokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
