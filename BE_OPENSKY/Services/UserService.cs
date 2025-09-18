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

    public async Task<UserResponseDTO> CreateGoogleUserAsync(GoogleUserRegisterDTO userDto)
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
            Role = RoleConstants.Customer,
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
            Status = user.Status,
            PhoneNumber = user.PhoneNumber,
            AvatarURL = user.AvatarURL,
            CreatedAt = user.CreatedAt
        };
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
            PhoneNumber = null, // Không bắt buộc trong đăng ký
            Role = role,
            ProviderId = null, // Không bắt buộc trong đăng ký
            AvatarURL = null, // Không bắt buộc trong đăng ký
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
            Status = user.Status,
            PhoneNumber = user.PhoneNumber,
            AvatarURL = user.AvatarURL,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponseDTO> CreateAdminUserAsync(AdminCreateUserDTO userDto)
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
            CitizenId = userDto.CitizenId,
            dob = userDto.dob,
            Role = userDto.Role,
            ProviderId = null,
            AvatarURL = null,
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
            Status = user.Status,
            PhoneNumber = user.PhoneNumber,
            CitizenId = user.CitizenId,
            dob = user.dob,
            AvatarURL = user.AvatarURL,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserResponseDTO> CreateAdminUserWithAvatarAsync(AdminCreateUserDTO userDto, string? avatarUrl)
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
            CitizenId = userDto.CitizenId,
            dob = userDto.dob,
            Role = userDto.Role,
            ProviderId = null,
            AvatarURL = avatarUrl, // Set avatar URL nếu có
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
            Status = user.Status,
            PhoneNumber = user.PhoneNumber,
            CitizenId = user.CitizenId,
            dob = user.dob,
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

    public async Task<PaginatedUsersResponseDTO> SearchUsersPaginatedAsync(int page = 1, int limit = 10, List<string>? roles = null, string? keyword = null)
    {
        page = Math.Max(1, page);
        limit = Math.Max(1, Math.Min(100, limit));

        var query = _context.Users.AsQueryable();

        // Lọc theo nhiều roles
        if (roles != null && roles.Any())
        {
            query = query.Where(u => roles.Contains(u.Role));
        }

        // Tìm kiếm theo keyword (FullName, Email, PhoneNumber)
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u =>
                u.FullName.Contains(keyword) ||
                u.Email.Contains(keyword) ||
                u.PhoneNumber.Contains(keyword));
        }

        var totalUsers = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalUsers / limit);

        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
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

        return new PaginatedUsersResponseDTO
        {
            Users = users,
            CurrentPage = page,
            PageSize = limit,
            TotalUsers = totalUsers,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<PaginatedUsersResponseDTO> GetUsersPaginatedAsync(int page = 1, int limit = 10, List<string>? roles = null){
        page = Math.Max(1, page);
        limit = Math.Max(1, Math.Min(100, limit));

        var query = _context.Users.AsQueryable();

        // Lọc theo nhiều roles
        if (roles != null && roles.Any())
        {
            query = query.Where(u => roles.Contains(u.Role));
        }

        var totalUsers = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalUsers / limit);

        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
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

        return new PaginatedUsersResponseDTO
        {
            Users = users,
            CurrentPage = page,
            PageSize = limit,
            TotalUsers = totalUsers,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
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
            dob = user.dob,
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

        if (updateDto.dob.HasValue)
            user.dob = updateDto.dob;

        await _context.SaveChangesAsync();

        return new ProfileResponseDTO
        {
            UserID = user.UserID,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            CitizenId = user.CitizenId,
            dob = user.dob,
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
            dob = user.dob,
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

    // Admin methods
    public async Task<UserResponseDTO?> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        return new UserResponseDTO
        {
            UserID = user.UserID,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Status = user.Status,
            PhoneNumber = user.PhoneNumber,
            CitizenId = user.CitizenId,
            dob = user.dob,
            AvatarURL = user.AvatarURL,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<bool> UpdateUserStatusAsync(Guid userId, UserStatus status, Guid adminId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        // Không cho phép Admin tự thay đổi status của mình
        if (userId == adminId)
        {
            throw new ArgumentException("Không thể thay đổi trạng thái của chính mình");
        }

        // Không cho phép thay đổi status của Admin khác
        if (user.Role == RoleConstants.Admin)
        {
            throw new ArgumentException("Không thể thay đổi trạng thái của Admin khác");
        }

        user.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }
}


