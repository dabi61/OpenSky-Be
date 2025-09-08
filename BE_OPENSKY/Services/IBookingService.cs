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
        
        // Phân trang booking
        Task<PaginatedBookingsResponseDTO> GetBookingsPaginatedAsync(int page = 1, int limit = 10, string? status = null, Guid? userId = null, Guid? hotelId = null);
        
        // Tìm kiếm booking
        Task<PaginatedBookingsResponseDTO> SearchBookingsAsync(BookingSearchDTO searchDto);
        
        // Hotel xem booking của khách sạn với phân trang
        Task<PaginatedBookingsResponseDTO> GetHotelBookingsPaginatedAsync(Guid hotelId, Guid userId, int page = 1, int limit = 10, string? status = null);
        
        // Kiểm tra phòng có sẵn nâng cao
        Task<RoomAvailabilityResponseDTO> CheckRoomAvailabilityAsync(RoomAvailabilityCheckDTO checkDto);
        
        // Thống kê booking
        Task<BookingStatsDTO> GetBookingStatsAsync(Guid? hotelId = null, DateTime? fromDate = null, DateTime? toDate = null);
        
        // Cập nhật trạng thái thanh toán booking
        Task<bool> UpdateBookingPaymentStatusAsync(Guid billId, string paymentStatus);
    }
}
