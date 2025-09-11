using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.DTOs
{
    // DTO cho request đặt phòng (chỉ những field user cần nhập)
    public class CreateHotelBookingRequestDTO
    {
        [Required]
        public Guid RoomID { get; set; }
        
        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }
    }

    // DTO cho internal use (có đầy đủ thông tin guest)
    public class CreateHotelBookingDTO
    {
        [Required]
        public Guid RoomID { get; set; }
        
        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }
        
    }

    // DTO cho response booking (tổng quát)
    public class BookingResponseDTO
    {
        public Guid BookingID { get; set; }
        public Guid UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        
        // Hotel booking info
        public Guid? HotelID { get; set; }
        public string? HotelName { get; set; }
        
        // Common info
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public Guid? BillID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // DTO cho danh sách booking (chỉ các trường cơ bản)
    public class BookingSummaryDTO
    {
        public Guid BookingID { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public Guid? BillID { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO cho cập nhật trạng thái booking
    public class UpdateBookingStatusDTO
    {
        [Required]
        public string Status { get; set; } = string.Empty; // "Confirmed", "Cancelled", "Completed", "Refunded"
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [StringLength(50)]
        public string? PaymentMethod { get; set; }
        
        [StringLength(100)]
        public string? PaymentStatus { get; set; }
    }

    // DTO cho phân trang danh sách booking (tổng quát)
    public class PaginatedBookingsResponseDTO
    {
        public List<BookingSummaryDTO> Bookings { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalBookings { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // DTO cho tìm kiếm booking
    public class BookingSearchDTO
    {
        public string? Query { get; set; } // Tìm theo tên khách, email, số điện thoại
        public string? Status { get; set; } // Pending, Confirmed, Cancelled, Completed, Refunded
        public DateTime? FromDate { get; set; } // Từ ngày check-in
        public DateTime? ToDate { get; set; } // Đến ngày check-out
        public Guid? HotelId { get; set; } // Lọc theo khách sạn
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public string? SortBy { get; set; } = "CreatedAt"; // CreatedAt, CheckInDate
        public string? SortOrder { get; set; } = "desc"; // asc, desc
    }

    // DTO cho kiểm tra phòng có sẵn
    public class RoomAvailabilityCheckDTO
    {
        [Required]
        public Guid RoomId { get; set; }
        
        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }
    }

    // DTO cho response kiểm tra phòng có sẵn
    public class RoomAvailabilityResponseDTO
    {
        public bool IsAvailable { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<BookingConflictDTO> Conflicts { get; set; } = new();
        public decimal Price { get; set; }
        public int NumberOfNights { get; set; }
        public decimal TotalPrice { get; set; }
    }

    // DTO cho xung đột booking
    public class BookingConflictDTO
    {
        public Guid BookingId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // DTO cho thống kê booking
    public class BookingStatsDTO
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int RefundedBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal ConfirmedRevenue { get; set; }
        public decimal CompletedRevenue { get; set; }
        public List<MonthlyStatsDTO> MonthlyStats { get; set; } = new();
    }

    // DTO cho thống kê theo tháng
    public class MonthlyStatsDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Bookings { get; set; }
        public decimal Revenue { get; set; }
    }


    public class PaymentResultDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
    }

    // DTO cho QR Payment (Test đơn giản)
    public class QRPaymentRequestDTO
    {
        [Required]
        public Guid BillId { get; set; }
    }

    public class QRPaymentResponseDTO
    {
        public string QRCode { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public Guid BillId { get; set; }
        public decimal Amount { get; set; }
        public string OrderDescription { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class QRPaymentStatusDTO
    {
        public string Status { get; set; } = string.Empty; // "Pending", "Paid", "Expired"
        public string Message { get; set; } = string.Empty;
        public DateTime? PaidAt { get; set; }
    }
}
