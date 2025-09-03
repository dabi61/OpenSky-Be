namespace BE_OPENSKY.Services;

public interface IUserService
{
    Task<UserResponseDTO> CreateAsync(UserRegisterDTO userDto);
    Task<string?> LoginAsync(LoginRequestDTO loginDto);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDTO changePasswordDto);
    Task<User?> GetByEmailAsync(string email);
}


