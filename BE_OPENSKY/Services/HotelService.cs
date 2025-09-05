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

    // New hotel owner methods
    public async Task<HotelDetailResponseDTO?> GetHotelDetailAsync(Guid hotelId, int page = 1, int limit = 10)
    {
        var hotel = await _context.Hotels
            .Include(h => h.User)
            .FirstOrDefaultAsync(h => h.HotelID == hotelId && h.Status == HotelStatus.Active);

        if (hotel == null) return null;

        // Get hotel images
        var images = await _context.Images
            .Where(i => i.TableType == TableTypeImage.Hotel && i.TypeID == hotelId)
            .OrderBy(i => i.CreatedAt)
            .Select(i => i.URL)
            .ToListAsync();

        // Get paginated rooms
        var totalRooms = await _context.HotelRooms.CountAsync(r => r.HotelID == hotelId);
        var rooms = await _context.HotelRooms
            .Where(r => r.HotelID == hotelId)
            .OrderBy(r => r.RoomName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(r => new RoomSummaryDTO
            {
                RoomID = r.RoomID,
                RoomName = r.RoomName,
                RoomType = r.RoomType,
                Price = r.Price,
                MaxPeople = r.MaxPeople,
                FirstImage = _context.Images
                    .Where(i => i.TableType == TableTypeImage.RoomHotel && i.TypeID == r.RoomID)
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.URL)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return new HotelDetailResponseDTO
        {
            HotelID = hotel.HotelID,
            UserID = hotel.UserID,
            Email = hotel.Email,
            HotelName = hotel.HotelName,
            Description = hotel.Description,
            Address = hotel.Address,
            District = hotel.District,
            Coordinates = hotel.Coordinates,
            Star = hotel.Star,
            Status = hotel.Status.ToString(),
            CreatedAt = hotel.CreatedAt,
            Images = images,
            Rooms = rooms,
            TotalRooms = totalRooms
        };
    }

    public async Task<bool> UpdateHotelAsync(Guid hotelId, Guid userId, UpdateHotelDTO updateDto)
    {
        var hotel = await _context.Hotels
            .FirstOrDefaultAsync(h => h.HotelID == hotelId && h.UserID == userId && h.Status == HotelStatus.Active);

        if (hotel == null) return false;

        // Update only non-null fields
        if (!string.IsNullOrWhiteSpace(updateDto.HotelName))
            hotel.HotelName = updateDto.HotelName;
        
        if (updateDto.Description != null)
            hotel.Description = updateDto.Description;
        
        if (!string.IsNullOrWhiteSpace(updateDto.Address))
            hotel.Address = updateDto.Address;
        
        if (!string.IsNullOrWhiteSpace(updateDto.District))
            hotel.District = updateDto.District;
        
        if (updateDto.Coordinates != null)
            hotel.Coordinates = updateDto.Coordinates;
        
        if (updateDto.Star.HasValue)
            hotel.Star = updateDto.Star.Value;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsHotelOwnerAsync(Guid hotelId, Guid userId)
    {
        return await _context.Hotels
            .AnyAsync(h => h.HotelID == hotelId && h.UserID == userId && h.Status == HotelStatus.Active);
    }

    // Room management methods
    public async Task<Guid> CreateRoomAsync(Guid hotelId, Guid userId, CreateRoomDTO createRoomDto)
    {
        // Verify hotel ownership
        var hotel = await _context.Hotels
            .FirstOrDefaultAsync(h => h.HotelID == hotelId && h.UserID == userId && h.Status == HotelStatus.Active);

        if (hotel == null)
            throw new UnauthorizedAccessException("Bạn không có quyền thêm phòng cho khách sạn này");

        var room = new HotelRoom
        {
            RoomID = Guid.NewGuid(),
            HotelID = hotelId,
            RoomName = createRoomDto.RoomName,
            RoomType = createRoomDto.RoomType,
            Address = createRoomDto.Address,
            Price = createRoomDto.Price,
            MaxPeople = createRoomDto.MaxPeople
        };

        _context.HotelRooms.Add(room);
        await _context.SaveChangesAsync();

        return room.RoomID;
    }

    public async Task<RoomDetailResponseDTO?> GetRoomDetailAsync(Guid roomId)
    {
        var room = await _context.HotelRooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.RoomID == roomId);

        if (room == null) return null;

        // Get room images
        var images = await _context.Images
            .Where(i => i.TableType == TableTypeImage.RoomHotel && i.TypeID == roomId)
            .OrderBy(i => i.CreatedAt)
            .Select(i => i.URL)
            .ToListAsync();

        return new RoomDetailResponseDTO
        {
            RoomID = room.RoomID,
            HotelID = room.HotelID,
            HotelName = room.Hotel.HotelName,
            RoomName = room.RoomName,
            RoomType = room.RoomType,
            Address = room.Address,
            Price = room.Price,
            MaxPeople = room.MaxPeople,
            CreatedAt = DateTime.UtcNow, // Using current time since CreatedAt doesn't exist in current model
            UpdatedAt = DateTime.UtcNow, // Using current time since UpdatedAt doesn't exist in current model
            Images = images
        };
    }

    public async Task<bool> UpdateRoomAsync(Guid roomId, Guid userId, UpdateRoomDTO updateDto)
    {
        var room = await _context.HotelRooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.RoomID == roomId && r.Hotel.UserID == userId && r.Hotel.Status == HotelStatus.Active);

        if (room == null) return false;

        // Update only non-null fields
        if (!string.IsNullOrWhiteSpace(updateDto.RoomName))
            room.RoomName = updateDto.RoomName;
        
        if (!string.IsNullOrWhiteSpace(updateDto.RoomType))
            room.RoomType = updateDto.RoomType;
        
        if (!string.IsNullOrWhiteSpace(updateDto.Address))
            room.Address = updateDto.Address;
        
        if (updateDto.Price.HasValue)
            room.Price = updateDto.Price.Value;
        
        if (updateDto.MaxPeople.HasValue)
            room.MaxPeople = updateDto.MaxPeople.Value;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRoomAsync(Guid roomId, Guid userId)
    {
        var room = await _context.HotelRooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.RoomID == roomId && r.Hotel.UserID == userId && r.Hotel.Status == HotelStatus.Active);

        if (room == null) return false;

        // Delete all images for this room
        var roomImages = await _context.Images
            .Where(i => i.TableType == TableTypeImage.RoomHotel && i.TypeID == roomId)
            .ToListAsync();
        if (roomImages.Any())
        {
            _context.Images.RemoveRange(roomImages);
        }

        _context.HotelRooms.Remove(room);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PaginatedRoomsResponseDTO> GetHotelRoomsAsync(Guid hotelId, int page = 1, int limit = 10)
    {
        var totalRooms = await _context.HotelRooms.CountAsync(r => r.HotelID == hotelId);
        var totalPages = (int)Math.Ceiling((double)totalRooms / limit);

        var rooms = await _context.HotelRooms
            .Where(r => r.HotelID == hotelId)
            .OrderBy(r => r.RoomName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(r => new RoomSummaryDTO
            {
                RoomID = r.RoomID,
                RoomName = r.RoomName,
                RoomType = r.RoomType,
                Price = r.Price,
                MaxPeople = r.MaxPeople,
                FirstImage = _context.Images
                    .Where(i => i.TableType == TableTypeImage.RoomHotel && i.TypeID == r.RoomID)
                    .OrderBy(i => i.CreatedAt)
                    .Select(i => i.URL)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return new PaginatedRoomsResponseDTO
        {
            Rooms = rooms,
            CurrentPage = page,
            PageSize = limit,
            TotalRooms = totalRooms,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<bool> IsRoomOwnerAsync(Guid roomId, Guid userId)
    {
        return await _context.HotelRooms
            .Include(r => r.Hotel)
            .AnyAsync(r => r.RoomID == roomId && r.Hotel.UserID == userId && r.Hotel.Status == HotelStatus.Active);
    }
}
