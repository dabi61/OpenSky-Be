using BE_OPENSKY.Data;
using Microsoft.EntityFrameworkCore;

namespace BE_OPENSKY.Services;

public class HotelService : IHotelService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserService _userService;

    public HotelService(ApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<Guid> CreateHotelApplicationAsync(Guid userId, HotelApplicationDTO applicationDto)
    {
        // Kiểm tra user đã có hotel chờ duyệt chưa
        var existingPendingHotel = await _context.Hotels
            .FirstOrDefaultAsync(h => h.UserID == userId && h.Status == HotelStatus.Inactive);

        if (existingPendingHotel != null)
        {
            throw new InvalidOperationException("Bạn đã có đơn đăng ký khách sạn đang chờ duyệt. Vui lòng chờ xử lý.");
        }

        // Lấy thông tin user để lấy email
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new InvalidOperationException("Không tìm thấy thông tin người dùng");
        }

        var hotel = new Hotel
        {
            HotelID = Guid.NewGuid(),
            UserID = userId,
            Email = user.Email, // Sử dụng email của user
            HotelName = applicationDto.HotelName,
            Address = applicationDto.Address,
            District = applicationDto.District,
            Coordinates = applicationDto.Coordinates,
            Description = applicationDto.Description,
            Star = applicationDto.Star,
            Status = HotelStatus.Inactive, // Chờ duyệt
            CreatedAt = DateTime.UtcNow
        };

        _context.Hotels.Add(hotel);
        await _context.SaveChangesAsync();

        return hotel.HotelID;
    }

    public async Task<List<PendingHotelResponseDTO>> GetPendingHotelsAsync()
    {
        var pendingHotels = await _context.Hotels
            .Include(h => h.User)
            .Where(h => h.Status == HotelStatus.Inactive)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new PendingHotelResponseDTO
            {
                HotelID = h.HotelID,
                UserID = h.UserID,
                UserEmail = h.User.Email,
                UserFullName = h.User.FullName,
                HotelName = h.HotelName,
                Address = h.Address,
                District = h.District,
                Coordinates = h.Coordinates,
                Description = h.Description,
                Star = h.Star,
                Status = h.Status.ToString(),
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();

        return pendingHotels;
    }

    public async Task<PendingHotelResponseDTO?> GetHotelByIdAsync(Guid hotelId)
    {
        var hotel = await _context.Hotels
            .Include(h => h.User)
            .FirstOrDefaultAsync(h => h.HotelID == hotelId);

        if (hotel == null) return null;

        return new PendingHotelResponseDTO
        {
            HotelID = hotel.HotelID,
            UserID = hotel.UserID,
            UserEmail = hotel.User.Email,
            UserFullName = hotel.User.FullName,
            HotelName = hotel.HotelName,
            Address = hotel.Address,
            District = hotel.District,
            Coordinates = hotel.Coordinates,
            Description = hotel.Description,
            Star = hotel.Star,
            Status = hotel.Status.ToString(),
            CreatedAt = hotel.CreatedAt
        };
    }

    public async Task<bool> ApproveHotelAsync(Guid hotelId, Guid adminId)
    {
        var hotel = await _context.Hotels
            .FirstOrDefaultAsync(h => h.HotelID == hotelId && h.Status == HotelStatus.Inactive);

        if (hotel == null) return false;

        // Chuyển hotel status thành Active
        hotel.Status = HotelStatus.Active;

        // Chuyển user role thành Hotel
        var success = await _userService.ChangeUserRoleAsync(hotel.UserID, RoleConstants.Hotel);
        if (!success)
        {
            throw new InvalidOperationException("Không thể chuyển role cho user");
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectHotelAsync(Guid hotelId)
    {
        var hotel = await _context.Hotels
            .FirstOrDefaultAsync(h => h.HotelID == hotelId && h.Status == HotelStatus.Inactive);

        if (hotel == null) return false;

        // Xóa hotel record thay vì chuyển status
        _context.Hotels.Remove(hotel);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<PendingHotelResponseDTO>> GetUserHotelsAsync(Guid userId)
    {
        var userHotels = await _context.Hotels
            .Where(h => h.UserID == userId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new PendingHotelResponseDTO
            {
                HotelID = h.HotelID,
                UserID = h.UserID,
                UserEmail = "", // Không cần thiết cho user xem hotel của chính mình
                UserFullName = "",
                HotelName = h.HotelName,
                Address = h.Address,
                District = h.District,
                Coordinates = h.Coordinates,
                Description = h.Description,
                Star = h.Star,
                Status = h.Status.ToString(),
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();

        return userHotels;
    }
}
