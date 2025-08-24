namespace BE_OPENSKY.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly JwtHelper _jwtHelper;

    public UserService(IUserRepository userRepository, IMapper mapper, JwtHelper jwtHelper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _jwtHelper = jwtHelper;
    }

        public async Task<UserResponseDTO?> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            return user != null ? _mapper.Map<UserResponseDTO>(user) : null;
        }

        public async Task<IEnumerable<UserResponseDTO>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserResponseDTO>>(users);
        }

    public async Task<UserResponseDTO> CreateAsync(UserRegisterDTO userDto)
    {
        if (await _userRepository.ExistsByEmailAsync(userDto.Email))
            throw new InvalidOperationException("Email already exists");

        var user = _mapper.Map<User>(userDto);
        user.PassWord = PasswordHelper.HashPassword(userDto.Password);

        var createdUser = await _userRepository.CreateAsync(user);
        return _mapper.Map<UserResponseDTO>(createdUser);
    }

    public async Task<UserResponseDTO?> UpdateAsync(int id, UserUpdateDTO userDto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return null;

        _mapper.Map(userDto, user);
        var updatedUser = await _userRepository.UpdateAsync(user);
        return _mapper.Map<UserResponseDTO>(updatedUser);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _userRepository.DeleteAsync(id);
    }

    public async Task<string?> LoginAsync(UserLoginDTO loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);
        if (user == null || !PasswordHelper.VerifyPassword(loginDto.Password, user.PassWord))
            return null;

        return _jwtHelper.GenerateToken(user);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO changePasswordDto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return false;

        if (!PasswordHelper.VerifyPassword(changePasswordDto.CurrentPassword, user.PassWord))
            return false;

        user.PassWord = PasswordHelper.HashPassword(changePasswordDto.NewPassword);
        await _userRepository.UpdateAsync(user);
        return true;
    }
}
