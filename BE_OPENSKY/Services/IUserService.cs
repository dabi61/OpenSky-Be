namespace BE_OPENSKY.Services;

public interface IUserService
{
    Task<UserResponseDTO?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserResponseDTO>> GetAllAsync();
    Task<UserResponseDTO> CreateAsync(UserRegisterDTO userDto);
    Task<UserResponseDTO?> UpdateAsync(Guid id, UserUpdateDTO userDto);
    Task<bool> DeleteAsync(Guid id);
    Task<string?> LoginAsync(UserLoginDTO loginDto);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDTO changePasswordDto);
}
