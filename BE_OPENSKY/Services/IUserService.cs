namespace BE_OPENSKY.Services;

public interface IUserService
{
    Task<UserResponseDTO?> GetByIdAsync(int id);
    Task<IEnumerable<UserResponseDTO>> GetAllAsync();
    Task<UserResponseDTO> CreateAsync(UserRegisterDTO userDto);
    Task<UserResponseDTO?> UpdateAsync(int id, UserUpdateDTO userDto);
    Task<bool> DeleteAsync(int id);
    Task<string?> LoginAsync(UserLoginDTO loginDto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO changePasswordDto);
}
