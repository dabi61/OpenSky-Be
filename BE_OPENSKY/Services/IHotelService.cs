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
    Task<HotelDetailResponseDTO?> GetHotelDetailAsync(Guid hotelId, int page = 1, int limit = 10);
    Task<bool> UpdateHotelAsync(Guid hotelId, Guid userId, UpdateHotelDTO updateDto);
    Task<bool> IsHotelOwnerAsync(Guid hotelId, Guid userId);
    
    // Room management methods
    Task<Guid> CreateRoomAsync(Guid hotelId, Guid userId, CreateRoomDTO createRoomDto);
    Task<RoomDetailResponseDTO?> GetRoomDetailAsync(Guid roomId);
    Task<bool> UpdateRoomAsync(Guid roomId, Guid userId, UpdateRoomDTO updateDto);
    Task<bool> DeleteRoomAsync(Guid roomId, Guid userId);
    Task<PaginatedRoomsResponseDTO> GetHotelRoomsAsync(Guid hotelId, int page = 1, int limit = 10);
    Task<bool> IsRoomOwnerAsync(Guid roomId, Guid userId);
    
    // Tìm kiếm và lọc khách sạn
    Task<HotelSearchResponseDTO> SearchHotelsAsync(HotelSearchDTO searchDto);
    
    // Quản lý trạng thái phòng
    Task<bool> UpdateRoomStatusAsync(Guid roomId, Guid userId, UpdateRoomStatusDTO updateDto);
    Task<RoomStatusListDTO> GetRoomStatusListAsync(Guid hotelId, string? status = null);
}
