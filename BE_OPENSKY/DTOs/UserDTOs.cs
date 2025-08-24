namespace BE_OPENSKY.DTOs
{
    public class UserLoginDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserRegisterDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Role { get; set; } // Optional: if not provided, defaults to Customer
        public string? NumberPhone { get; set; }
        public string? CitizenId { get; set; }
        public DateTime? DoB { get; set; }
        public string? AvatarURL { get; set; }
    }

    public class UserResponseDTO
    {
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? NumberPhone { get; set; }
        public string? CitizenId { get; set; }
        public DateTime? DoB { get; set; }
        public string? AvatarURL { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserUpdateDTO
    {
        public string? FullName { get; set; }
        public string? NumberPhone { get; set; }
        public string? CitizenId { get; set; }
        public DateTime? DoB { get; set; }
        public string? AvatarURL { get; set; }
    }

    public class ChangePasswordDTO
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
