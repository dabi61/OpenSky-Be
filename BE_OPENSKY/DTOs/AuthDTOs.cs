using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.DTOs
{
    // DTO cho phản hồi khi đăng nhập/làm mới token thành công
    public class AuthResponseDTO
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpires { get; set; }
        public DateTime RefreshTokenExpires { get; set; }
        public UserResponseDTO User { get; set; } = null!;
    }

    // Thông tin user trả về cho client
    public class UserResponseDTO
    {
        public Guid UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? CitizenId { get; set; }
        public DateOnly? DoB { get; set; }
        public string? AvatarURL { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO đăng nhập
    public class UserLoginDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // DTO đăng ký tài khoản
    public class UserRegisterDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string Role { get; set; } = "Customer";
        public string? ProviderId { get; set; }
        public string? AvatarURL { get; set; }
    }

    // DTO đổi mật khẩu
    public class ChangePasswordDTO
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }

    // DTO cho yêu cầu làm mới token
    public class RefreshTokenRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    // DTO cho yêu cầu đăng xuất
    public class LogoutRequestDTO
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    // DTO cho yêu cầu đăng nhập (mở rộng từ existing)
    public class LoginRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
