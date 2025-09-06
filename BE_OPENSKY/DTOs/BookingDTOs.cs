using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.DTOs
{
    // DTO cho tạo booking hotel
    public class CreateHotelBookingDTO
    {
        [Required]
        public Guid RoomID { get; set; }
        
        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }
        
        [StringLength(100)]
        public string? GuestName { get; set; }
        
        [StringLength(20)]
        public string? GuestPhone { get; set; }
        
        [StringLength(100)]
        public string? GuestEmail { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    // DTO cho tạo booking tour (sẽ dùng sau)
    public class CreateTourBookingDTO
    {
        [Required]
        public Guid TourID { get; set; }
        
        [Required]
        public Guid ScheduleID { get; set; }
        
        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }
        
        [Required]
        [Range(1, 10)]
        public int NumberOfPeople { get; set; }
        
        [StringLength(100)]
        public string? GuestName { get; set; }
        
        [StringLength(20)]
        public string? GuestPhone { get; set; }
        
        [StringLength(100)]
        public string? GuestEmail { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }

    // DTO cho phản hồi booking
    public class BookingResponseDTO
    {
        public Guid BookingID { get; set; }
        public Guid UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string BookingType { get; set; } = string.Empty;
        
        // Hotel booking info
        public Guid? HotelID { get; set; }
        public string? HotelName { get; set; }
        public Guid? RoomID { get; set; }
        public string? RoomName { get; set; }
        public string? RoomType { get; set; }
        
        // Tour booking info (sẽ dùng sau)
        public Guid? TourID { get; set; }
        public string? TourName { get; set; }
        public Guid? ScheduleID { get; set; }
        public string? ScheduleName { get; set; }
        
        // Common info
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? GuestName { get; set; }
        public string? GuestPhone { get; set; }
        public string? GuestEmail { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // DTO cho danh sách booking
    public class BookingListDTO
    {
        public List<BookingResponseDTO> Bookings { get; set; } = new();
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int RefundedBookings { get; set; }
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
}
