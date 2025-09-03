using BE_OPENSKY.Data;

namespace BE_OPENSKY.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtHelper _jwtHelper;

    public UserService(ApplicationDbContext context, JwtHelper jwtHelper)
    {
        _context = context;
        _jwtHelper = jwtHelper;
    }

    public async Task<UserResponseDTO> CreateAsync(UserRegisterDTO userDto)
    {
        // Kiểm tra email đã tồn tại chưa
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email đã được sử dụng bởi tài khoản khác");
        }

        var user = new User
        {
            UserID = Guid.NewGuid(),
            Email = userDto.Email,
            Password = PasswordHelper.HashPassword(userDto.Password),
            FullName = userDto.FullName,
            Role = string.IsNullOrEmpty(userDto.Role) ? RoleConstants.Customer : userDto.Role,
            ProviderId = userDto.ProviderId,
            AvatarURL = userDto.AvatarURL,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new UserResponseDTO
        {
            UserID = user.UserID,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            AvatarURL = user.AvatarURL,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<string?> LoginAsync(LoginRequestDTO loginDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
        if (user == null) return null;
        if (!PasswordHelper.VerifyPassword(loginDto.Password, user.Password)) return null;
        return _jwtHelper.GenerateAccessToken(user);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDTO changePasswordDto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("Không tìm thấy tài khoản người dùng");
        }
        
        if (!PasswordHelper.VerifyPassword(changePasswordDto.CurrentPassword, user.Password))
        {
            return false; // Mật khẩu hiện tại không đúng
        }
        
        user.Password = PasswordHelper.HashPassword(changePasswordDto.NewPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.Email == email)!;
    }
}


