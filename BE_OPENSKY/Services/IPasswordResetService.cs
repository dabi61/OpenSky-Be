namespace BE_OPENSKY.Services;

public interface IPasswordResetService
{
    Task<string> GenerateResetTokenAsync(string email);
    Task<bool> ValidateResetTokenAsync(string token);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task<bool> InvalidateTokenAsync(string token);
}
