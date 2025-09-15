using BE_OPENSKY.Models;

namespace BE_OPENSKY.DTOs
{
    // DTO cho tạo tour booking
    public class CreateTourBookingDTO
    {
        public Guid TourID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Notes { get; set; }
    }

    // DTO cho cập nhật tour booking
    public class UpdateTourBookingDTO
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
    }

    // DTO cho response tour booking
    public class TourBookingResponseDTO
    {
        public Guid BookingID { get; set; }
        public Guid UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid TourID { get; set; }
        public string TourName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Thông tin tour
        public TourInfoDTO? TourInfo { get; set; }
    }

    // DTO cho thông tin tour
    public class TourInfoDTO
    {
        public Guid TourID { get; set; }
        public string TourName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Duration { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }


    // DTO cho danh sách tour booking
    public class TourBookingListResponseDTO
    {
        public List<TourBookingResponseDTO> Bookings { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalPages { get; set; }
    }
}
