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
            Province = applicationDto.Province,
            Latitude = applicationDto.Latitude,
            Longitude = applicationDto.Longitude,
            Description = applicationDto.Description,
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
                Province = h.Province,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
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
            Province = hotel.Province,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
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
                Province = h.Province,
                Latitude = h.Latitude,
                Longitude = h.Longitude,
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
            Province = hotel.Province,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
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
        
        if (!string.IsNullOrWhiteSpace(updateDto.Province))
            hotel.Province = updateDto.Province;
        
        if (updateDto.Latitude.HasValue)
            hotel.Latitude = updateDto.Latitude.Value;
        
        if (updateDto.Longitude.HasValue)
            hotel.Longitude = updateDto.Longitude.Value;
        
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
            MaxPeople = createRoomDto.MaxPeople,
            Status = RoomStatus.Available // Đảm bảo phòng mới luôn có trạng thái Available
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

    public async Task<HotelSearchResponseDTO> SearchHotelsAsync(HotelSearchDTO searchDto)
    {
        var query = _context.Hotels
            .Where(h => h.Status == HotelStatus.Active) // Chỉ lấy khách sạn đã được duyệt
            .AsQueryable();

        // Tìm kiếm theo tên khách sạn
        if (!string.IsNullOrWhiteSpace(searchDto.Query))
        {
            var searchTerm = searchDto.Query.ToLower();
            query = query.Where(h => h.HotelName.ToLower().Contains(searchTerm) ||
                                   h.Description != null && h.Description.ToLower().Contains(searchTerm));
        }

        // Lọc theo tỉnh
        if (!string.IsNullOrWhiteSpace(searchDto.Province))
        {
            query = query.Where(h => h.Province.ToLower().Contains(searchDto.Province.ToLower()));
        }

        // Lọc theo địa chỉ
        if (!string.IsNullOrWhiteSpace(searchDto.Address))
        {
            var addressTerm = searchDto.Address.ToLower();
            query = query.Where(h => h.Address.ToLower().Contains(addressTerm));
        }

        // Lọc theo số sao
        if (searchDto.Stars != null && searchDto.Stars.Any())
        {
            query = query.Where(h => searchDto.Stars.Contains(h.Star));
        }

        // Lọc theo giá phòng (cần join với HotelRoom)
        if (searchDto.MinPrice.HasValue || searchDto.MaxPrice.HasValue)
        {
            var roomQuery = _context.HotelRooms.AsQueryable();
            
            if (searchDto.MinPrice.HasValue)
            {
                roomQuery = roomQuery.Where(r => r.Price >= searchDto.MinPrice.Value);
            }
            
            if (searchDto.MaxPrice.HasValue)
            {
                roomQuery = roomQuery.Where(r => r.Price <= searchDto.MaxPrice.Value);
            }

            var hotelIdsWithPriceFilter = await roomQuery
                .Select(r => r.HotelID)
                .Distinct()
                .ToListAsync();

            query = query.Where(h => hotelIdsWithPriceFilter.Contains(h.HotelID));
        }

        // Sắp xếp
        query = searchDto.SortBy?.ToLower() switch
        {
            "price" => searchDto.SortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(h => _context.HotelRooms.Where(r => r.HotelID == h.HotelID).Min(r => r.Price))
                : query.OrderBy(h => _context.HotelRooms.Where(r => r.HotelID == h.HotelID).Min(r => r.Price)),
            "star" => searchDto.SortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(h => h.Star)
                : query.OrderBy(h => h.Star),
            "createdat" => searchDto.SortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(h => h.CreatedAt)
                : query.OrderBy(h => h.CreatedAt),
            _ => searchDto.SortOrder?.ToLower() == "desc" 
                ? query.OrderByDescending(h => h.HotelName)
                : query.OrderBy(h => h.HotelName)
        };

        // Đếm tổng số kết quả
        var totalCount = await query.CountAsync();

        // Phân trang
        var skip = (searchDto.Page - 1) * searchDto.Limit;
        var hotels = await query
            .Skip(skip)
            .Take(searchDto.Limit)
            .ToListAsync();

        // Lấy thông tin bổ sung cho mỗi khách sạn
        var result = new List<HotelSearchResultDTO>();

        foreach (var hotel in hotels)
        {
            // Lấy ảnh khách sạn
            var images = await _context.Images
                .Where(i => i.TableType == TableTypeImage.Hotel && i.TypeID == hotel.HotelID)
                .Select(i => i.URL)
                .ToListAsync();

            // Lấy thông tin phòng
            var rooms = await _context.HotelRooms
                .Where(r => r.HotelID == hotel.HotelID)
                .ToListAsync();

            var minPrice = rooms.Any() ? rooms.Min(r => r.Price) : 0;
            var maxPrice = rooms.Any() ? rooms.Max(r => r.Price) : 0;
            var totalRooms = rooms.Count;
            var availableRooms = totalRooms; // Tạm thời coi tất cả phòng đều available

            result.Add(new HotelSearchResultDTO
            {
                HotelID = hotel.HotelID,
                HotelName = hotel.HotelName,
                Address = hotel.Address,
                Province = hotel.Province,
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude,
                Description = hotel.Description,
                Star = hotel.Star,
                Status = hotel.Status.ToString(),
                CreatedAt = hotel.CreatedAt,
                Images = images,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                TotalRooms = totalRooms,
                AvailableRooms = availableRooms
            });
        }

        var totalPages = (int)Math.Ceiling((double)totalCount / searchDto.Limit);

        return new HotelSearchResponseDTO
        {
            Hotels = result,
            TotalCount = totalCount,
            Page = searchDto.Page,
            Limit = searchDto.Limit,
            TotalPages = totalPages,
            HasNextPage = searchDto.Page < totalPages,
            HasPreviousPage = searchDto.Page > 1
        };
    }

    // Quản lý trạng thái phòng
    public async Task<bool> UpdateRoomStatusAsync(Guid roomId, Guid userId, UpdateRoomStatusDTO updateDto)
    {
        // Kiểm tra phòng có tồn tại không
        var room = await _context.HotelRooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.RoomID == roomId);

        if (room == null)
        {
            return false;
        }

        // Kiểm tra quyền sở hữu khách sạn
        if (room.Hotel.UserID != userId)
        {
            return false;
        }

        // Convert string status to enum
        if (!Enum.TryParse<RoomStatus>(updateDto.Status, true, out var roomStatus))
        {
            throw new ArgumentException($"Trạng thái không hợp lệ: {updateDto.Status}. Các trạng thái hợp lệ: Available, Occupied, Maintenance");
        }

        // Cập nhật trạng thái phòng
        room.Status = roomStatus;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<RoomStatusListDTO> GetRoomStatusListAsync(Guid hotelId, string? status = null)
    {
        // Kiểm tra khách sạn có tồn tại không
        var hotel = await _context.Hotels.FindAsync(hotelId);
        if (hotel == null)
        {
            throw new InvalidOperationException("Không tìm thấy khách sạn");
        }

        // Query phòng theo trạng thái
        var query = _context.HotelRooms
            .Where(r => r.HotelID == hotelId);

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<RoomStatus>(status, true, out var roomStatus))
            {
                query = query.Where(r => r.Status == roomStatus);
            }
            else
            {
                throw new ArgumentException($"Trạng thái không hợp lệ: {status}. Các trạng thái hợp lệ: Available, Occupied, Maintenance");
            }
        }

        var rooms = await query
            .OrderBy(r => r.RoomName)
            .ToListAsync();

        // Thống kê trạng thái
        var allRooms = await _context.HotelRooms
            .Where(r => r.HotelID == hotelId)
            .ToListAsync();

        var roomStatusList = new RoomStatusListDTO
        {
            Rooms = rooms.Select(r => new RoomStatusResponseDTO
            {
                RoomID = r.RoomID,
                RoomName = r.RoomName,
                RoomType = r.RoomType,
                Status = r.Status.ToString(),
                UpdatedAt = DateTime.UtcNow // Tạm thời dùng DateTime.UtcNow
            }).ToList(),
            TotalRooms = allRooms.Count,
            AvailableRooms = allRooms.Count(r => r.Status == RoomStatus.Available),
            OccupiedRooms = allRooms.Count(r => r.Status == RoomStatus.Occupied),
            MaintenanceRooms = allRooms.Count(r => r.Status == RoomStatus.Maintenance),
            OutOfOrderRooms = 0 // Tạm thời set = 0 vì không có OutOfOrder
        };

        return roomStatusList;
    }

    public async Task<List<string>> DeleteHotelImagesAsync(Guid hotelId, Guid userId, string action = "keep")
    {
        // Verify hotel ownership
        var hotel = await _context.Hotels
            .FirstOrDefaultAsync(h => h.HotelID == hotelId && h.UserID == userId && h.Status == HotelStatus.Active);

        if (hotel == null)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa ảnh của khách sạn này");

        var deletedUrls = new List<string>();

        if (action == "replace")
        {
            // Get current images
            var currentImages = await _context.Images
                .Where(i => i.TableType == TableTypeImage.Hotel && i.TypeID == hotelId)
                .ToListAsync();

            // Store URLs before deletion for response
            deletedUrls = currentImages.Select(i => i.URL).ToList();

            // Delete from Cloudinary first, then from database
            foreach (var image in currentImages)
            {
                try
                {
                    // Extract public ID from Cloudinary URL
                    // Cloudinary URL format: https://res.cloudinary.com/{cloud_name}/image/upload/v{version}/{public_id}.{format}
                    var uri = new Uri(image.URL);
                    var pathSegments = uri.AbsolutePath.Split('/');
                    var publicIdWithExtension = pathSegments[pathSegments.Length - 1];
                    var publicId = System.IO.Path.GetFileNameWithoutExtension(publicIdWithExtension);

                    // Delete from Cloudinary
                    using var scope = _context.GetService<IServiceScopeFactory>().CreateScope();
                    var cloudinaryService = scope.ServiceProvider.GetRequiredService<ICloudinaryService>();
                    await cloudinaryService.DeleteImageAsync(publicId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete image from Cloudinary: {image.URL}, Error: {ex.Message}");
                    // Continue with database deletion even if Cloudinary deletion fails
                }
            }

            // Delete from database
            _context.Images.RemoveRange(currentImages);
            await _context.SaveChangesAsync();
        }

        return deletedUrls;
    }

    public async Task<List<string>> DeleteRoomImagesAsync(Guid roomId, Guid userId, string action = "keep")
    {
        // Verify room ownership
        var room = await _context.HotelRooms
            .Include(r => r.Hotel)
            .FirstOrDefaultAsync(r => r.RoomID == roomId && r.Hotel.UserID == userId && r.Hotel.Status == HotelStatus.Active);

        if (room == null)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa ảnh của phòng này");

        var deletedUrls = new List<string>();

        if (action == "replace")
        {
            // Get current images
            var currentImages = await _context.Images
                .Where(i => i.TableType == TableTypeImage.RoomHotel && i.TypeID == roomId)
                .ToListAsync();

            // Store URLs before deletion for response
            deletedUrls = currentImages.Select(i => i.URL).ToList();

            // Delete from Cloudinary first, then from database
            foreach (var image in currentImages)
            {
                try
                {
                    // Extract public ID from Cloudinary URL
                    var uri = new Uri(image.URL);
                    var pathSegments = uri.AbsolutePath.Split('/');
                    var publicIdWithExtension = pathSegments[pathSegments.Length - 1];
                    var publicId = System.IO.Path.GetFileNameWithoutExtension(publicIdWithExtension);

                    // Delete from Cloudinary
                    using var scope = _context.GetService<IServiceScopeFactory>().CreateScope();
                    var cloudinaryService = scope.ServiceProvider.GetRequiredService<ICloudinaryService>();
                    await cloudinaryService.DeleteImageAsync(publicId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete image from Cloudinary: {image.URL}, Error: {ex.Message}");
                    // Continue with database deletion even if Cloudinary deletion fails
                }
            }

            // Delete from database
            _context.Images.RemoveRange(currentImages);
            await _context.SaveChangesAsync();
        }

        return deletedUrls;
    }
}
