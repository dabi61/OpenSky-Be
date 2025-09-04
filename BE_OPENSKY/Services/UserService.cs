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
        return await CreateWithRoleAsync(userDto, RoleConstants.Customer);
    }

    public async Task<UserResponseDTO> CreateWithRoleAsync(UserRegisterDTO userDto, string role)
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
            PhoneNumber = userDto.PhoneNumber,
            Role = role,
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

    public async Task<bool> ChangeUserRoleAsync(Guid userId, string newRole)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // Kiểm tra role hợp lệ
        if (!RoleConstants.AllRoles.Contains(newRole))
        {
            throw new ArgumentException("Role không hợp lệ");
        }

        user.Role = newRole;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserResponseDTO>> GetUsersAsync(string? role = null)
    {
        var query = _context.Users.AsQueryable();

        // Lọc theo role nếu có
        if (!string.IsNullOrEmpty(role))
        {
            query = query.Where(u => u.Role == role);
        }

        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Select(u => new UserResponseDTO
            {
                UserID = u.UserID,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                PhoneNumber = u.PhoneNumber,
                AvatarURL = u.AvatarURL,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return users;
    }

    public async Task<ProfileResponseDTO?> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        return new ProfileResponseDTO
        {
            UserID = user.UserID,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            CitizenId = user.CitizenId,
            DoB = user.DoB,
            AvatarURL = user.AvatarURL,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<ProfileResponseDTO> UpdateProfileAsync(Guid userId, UpdateProfileDTO updateDto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Không tìm thấy tài khoản người dùng");

        // Cập nhật các thông tin được cung cấp
        if (!string.IsNullOrWhiteSpace(updateDto.FullName))
            user.FullName = updateDto.FullName;

        if (!string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
            user.PhoneNumber = updateDto.PhoneNumber;

        if (!string.IsNullOrWhiteSpace(updateDto.CitizenId))
            user.CitizenId = updateDto.CitizenId;

        if (updateDto.DoB.HasValue)
            user.DoB = updateDto.DoB;

        await _context.SaveChangesAsync();

        return new ProfileResponseDTO
        {
            UserID = user.UserID,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            CitizenId = user.CitizenId,
            DoB = user.DoB,
            AvatarURL = user.AvatarURL,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<ProfileResponseDTO> UpdateAvatarAsync(Guid userId, string avatarUrl)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Không tìm thấy tài khoản người dùng");

        user.AvatarURL = avatarUrl;
        await _context.SaveChangesAsync();

        return new ProfileResponseDTO
        {
            UserID = user.UserID,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            CitizenId = user.CitizenId,
            DoB = user.DoB,
            AvatarURL = user.AvatarURL,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string newPassword)
    {
        try
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("User ID không hợp lệ", nameof(userId));

            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Mật khẩu mới không được để trống", nameof(newPassword));

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản người dùng");

            // Hash mật khẩu mới
            user.Password = PasswordHelper.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Lỗi khi reset mật khẩu: {ex.Message}", ex);
        }
    }
}


