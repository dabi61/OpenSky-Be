using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface ITourBookingService
    {
        // Tạo tour booking
        Task<Guid> CreateTourBookingAsync(Guid userId, CreateTourBookingDTO createBookingDto);
        
        // Lấy tour booking theo ID
        Task<TourBookingResponseDTO?> GetTourBookingByIdAsync(Guid bookingId, Guid userId);
        
        // Lấy danh sách tour booking của user
        Task<TourBookingListResponseDTO> GetUserTourBookingsAsync(Guid userId, int page = 1, int size = 10);
        
        // Lấy tất cả tour booking (Admin)
        Task<TourBookingListResponseDTO> GetAllTourBookingsAsync(int page = 1, int size = 10);
        
        // Hủy tour booking
        Task<bool> CancelTourBookingAsync(Guid bookingId, Guid userId);
        
    }
}
