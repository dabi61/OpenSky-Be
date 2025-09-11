using BE_OPENSKY.DTOs;

namespace BE_OPENSKY.Services
{
    public interface IBookingService
    {
        // Customer đặt phòng
        Task<Guid> CreateHotelBookingAsync(Guid userId, CreateHotelBookingDTO createBookingDto);
        
        // Customer hủy booking
        Task<bool> CustomerCancelBookingAsync(Guid bookingId, Guid userId, string? reason = null);
        
        // Lấy thông tin booking theo ID
        Task<BookingResponseDTO?> GetBookingByIdAsync(Guid bookingId, Guid userId);
        
        // Phân trang booking
        Task<PaginatedBookingsResponseDTO> GetBookingsPaginatedAsync(int page = 1, int limit = 10, string? status = null, Guid? userId = null, Guid? hotelId = null);
        
        // Tìm kiếm booking
        Task<PaginatedBookingsResponseDTO> SearchBookingsAsync(BookingSearchDTO searchDto);
        
        // Kiểm tra phòng có sẵn nâng cao
        Task<RoomAvailabilityResponseDTO> CheckRoomAvailabilityAsync(RoomAvailabilityCheckDTO checkDto);
        
        // Thống kê booking
        Task<BookingStatsDTO> GetBookingStatsAsync(Guid? hotelId = null, DateTime? fromDate = null, DateTime? toDate = null);
        
        // Cập nhật trạng thái thanh toán booking
        Task<bool> UpdateBookingPaymentStatusAsync(Guid billId, string paymentStatus);
        
        // Cập nhật trạng thái booking
        Task<bool> UpdateBookingStatusAsync(Guid billId, string status);
        
        // Check-in booking (cập nhật RoomStatus thành Occupied)
        Task<bool> CheckInBookingAsync(Guid bookingId, Guid userId);
        
        // Check-out booking (cập nhật RoomStatus thành Available)
        Task<bool> CheckOutBookingAsync(Guid bookingId, Guid userId);
        
        // Tạo QR code thanh toán (test đơn giản)
        Task<QRPaymentResponseDTO> CreateQRPaymentAsync(Guid billId);
    }
}
