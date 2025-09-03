namespace BE_OPENSKY.Services;

public interface IUserService
{
    Task<UserResponseDTO> CreateAsync(UserRegisterDTO userDto);
    Task<UserResponseDTO> CreateWithRoleAsync(UserRegisterDTO userDto, string role);
    Task<string?> LoginAsync(LoginRequestDTO loginDto);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDTO changePasswordDto);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ChangeUserRoleAsync(Guid userId, string newRole);
    Task<List<UserResponseDTO>> GetUsersAsync(string? role = null);
}


