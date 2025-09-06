using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IBookingService
    {
        // Customer đặt phòng
        Task<Guid> CreateHotelBookingAsync(Guid userId, CreateHotelBookingDTO createBookingDto);
        
        // Customer xem booking của mình
        Task<BookingListDTO> GetMyBookingsAsync(Guid userId);
        
        // Hotel xem danh sách booking của khách sạn
        Task<BookingListDTO> GetHotelBookingsAsync(Guid hotelId, Guid userId);
        
        // Hotel xác nhận booking (tạo Bill + BillDetail)
        Task<bool> ConfirmBookingAsync(Guid bookingId, Guid userId);
        
        // Hotel hủy booking
        Task<bool> CancelBookingAsync(Guid bookingId, Guid userId, string? reason = null);
        
        // Customer hủy booking
        Task<bool> CustomerCancelBookingAsync(Guid bookingId, Guid userId, string? reason = null);
        
        // Lấy thông tin booking theo ID
        Task<BookingResponseDTO?> GetBookingByIdAsync(Guid bookingId, Guid userId);
        
        // Cập nhật trạng thái booking (cho admin)
        Task<bool> UpdateBookingStatusAsync(Guid bookingId, Guid userId, UpdateBookingStatusDTO updateDto);
    }
}
