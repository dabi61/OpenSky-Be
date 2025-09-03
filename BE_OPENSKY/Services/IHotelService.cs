namespace BE_OPENSKY.Services;

public interface IHotelService
{
    Task<Guid> CreateHotelApplicationAsync(Guid userId, HotelApplicationDTO applicationDto);
    Task<List<PendingHotelResponseDTO>> GetPendingHotelsAsync();
    Task<PendingHotelResponseDTO?> GetHotelByIdAsync(Guid hotelId);
    Task<bool> ApproveHotelAsync(Guid hotelId, Guid adminId);
    Task<bool> RejectHotelAsync(Guid hotelId);
    Task<List<PendingHotelResponseDTO>> GetUserHotelsAsync(Guid userId);
}
