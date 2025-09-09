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
        public UserStatus Status { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CitizenId { get; set; }
        public DateOnly? DoB { get; set; }
        public string? AvatarURL { get; set; }
        public DateTime CreatedAt { get; set; }
    }

// DTO cho phân trang danh sách users
public class PaginatedUsersResponseDTO
{
    public List<UserResponseDTO> Users { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalUsers { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

// DTO cho việc tạo tài khoản bởi Admin/Supervisor
public class CreateUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

// DTO cho Admin tạo user với role tùy chỉnh
public class AdminCreateUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

// DTO cho việc cập nhật status user
public class UpdateUserStatusDTO
{
    public UserStatus Status { get; set; }
}

// DTO cho đơn đăng ký mở khách sạn
public class HotelApplicationDTO
{
    public string HotelName { get; set; } = string.Empty; // Tên khách sạn
    public string Address { get; set; } = string.Empty; // Địa chỉ
    public string Province { get; set; } = string.Empty; // Tỉnh/Thành phố
    public string? Coordinates { get; set; } // Tọa độ
    public string? Description { get; set; } // Mô tả
    public int Star { get; set; } = 3; // Số sao (1-5)
}

// DTO trả về thông tin khách sạn chờ duyệt
public class PendingHotelResponseDTO
{
    public Guid HotelID { get; set; }
    public Guid UserID { get; set; }
    public string UserEmail { get; set; } = string.Empty; // Email của customer
    public string UserFullName { get; set; } = string.Empty; // Tên của customer
    public string HotelName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string? Coordinates { get; set; }
    public string? Description { get; set; }
    public int Star { get; set; }
    public string Status { get; set; } = string.Empty; // Inactive, Active
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
    }

    // DTO đăng ký tài khoản cho Google OAuth (có thêm các trường optional)
    public class GoogleUserRegisterDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? ProviderId { get; set; } // Chỉ dành cho Google OAuth
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

    // DTO cho cập nhật thông tin cá nhân
    public class UpdateProfileDTO
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CitizenId { get; set; }
        public DateOnly? DoB { get; set; }
    }

    // DTO cho cập nhật profile với avatar (multipart form data)
    public class UpdateProfileWithAvatarDTO
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CitizenId { get; set; }
        public string? DoB { get; set; } // String để dễ xử lý trong form
        public IFormFile? Avatar { get; set; }
    }

    // DTO cho upload avatar
    public class UploadAvatarDTO
    {
        [Required]
        public IFormFile Avatar { get; set; } = null!;
    }

    // DTO cho thông tin profile chi tiết
    public class ProfileResponseDTO
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

    // DTO cho quên mật khẩu
    public class ForgotPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    // DTO cho reset mật khẩu
    public class ResetPasswordDTO
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
