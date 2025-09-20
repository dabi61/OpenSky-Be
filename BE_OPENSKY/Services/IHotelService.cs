namespace BE_OPENSKY.Services;

public interface IHotelService
{
    // Existing admin/application methods
    Task<Guid> CreateHotelApplicationAsync(Guid userId, HotelApplicationDTO applicationDto);
    Task<List<PendingHotelResponseDTO>> GetPendingHotelsAsync();
    Task<PendingHotelResponseDTO?> GetHotelByIdAsync(Guid hotelId);
    Task<bool> ApproveHotelAsync(Guid hotelId, Guid adminId);
    Task<bool> RejectHotelAsync(Guid hotelId);
    Task<List<PendingHotelResponseDTO>> GetUserHotelsAsync(Guid userId);
    
    // New hotel owner methods
    Task<HotelDetailResponseDTO?> GetHotelDetailAsync(Guid hotelId);
    Task<bool> UpdateHotelAsync(Guid hotelId, Guid userId, UpdateHotelDTO updateDto);
    Task<bool> IsHotelOwnerAsync(Guid hotelId, Guid userId);
    Task<List<string>> DeleteHotelImagesAsync(Guid hotelId, Guid userId, string action = "keep");
    Task<List<string>> DeleteRoomImagesAsync(Guid roomId, Guid userId, string action = "keep");
    
    // Room management methods
    Task<Guid> CreateRoomAsync(Guid hotelId, Guid userId, CreateRoomDTO createRoomDto);
    Task<RoomDetailResponseDTO?> GetRoomDetailAsync(Guid roomId);
    Task<bool> UpdateRoomAsync(Guid roomId, Guid userId, UpdateRoomDTO updateDto);
    Task<PaginatedRoomsResponseDTO> GetHotelRoomsAsync(Guid hotelId, int page = 1, int limit = 10);
    Task<bool> IsRoomOwnerAsync(Guid roomId, Guid userId);
    
    // Tìm kiếm và lọc khách sạn
    Task<HotelSearchResponseDTO> SearchHotelsAsync(HotelSearchDTO searchDto);
    
    // Quản lý trạng thái phòng
    Task<bool> UpdateRoomStatusAsync(Guid roomId, Guid userId, UpdateRoomStatusDTO updateDto);
    Task<RoomStatusListDTO> GetRoomStatusListAsync(Guid hotelId, string? status = null);
    
    // Quản lý trạng thái hotel (ADMIN - SUPERVISOR)
    Task<PaginatedHotelsResponseDTO> GetHotelsByStatusAsync(HotelStatus status, int page, int size);
    Task<bool> UpdateHotelStatusAsync(Guid hotelId, string statusString);
    
    // Lấy khách sạn theo số sao (Public)
    Task<PaginatedHotelsResponseDTO> GetHotelsByStarAsync(int star, int page, int size);
    
    // Lấy khách sạn theo tỉnh/thành phố (Public)
    Task<PaginatedHotelsResponseDTO> GetHotelsByProvinceAsync(string province, int page, int size);
}
