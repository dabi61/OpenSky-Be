using BE_OPENSKY.Models;

namespace BE_OPENSKY.DTOs
{
    // DTO cho tạo refund request
    public class CreateRefundDTO
    {
        public Guid BillID { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // DTO cho pending refund request (lưu Redis)
    public class PendingRefundRequestDTO
    {
        public Guid BillID { get; set; }
        public Guid RequestedBy { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public int RefundPercentage { get; set; }
        public decimal RefundAmount { get; set; }
        public string PolicyDescription { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty; // Hotel hoặc Tour
        public Guid? HotelOwnerId { get; set; }
        public Guid? TourOwnerId { get; set; }
    }


    // DTO cho response refund
    public class RefundResponseDTO
    {
        public Guid RefundID { get; set; }
        public Guid BillID { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        // Thông tin refund policy
        public int RefundPercentage { get; set; }
        public decimal RefundAmount { get; set; }
        public string PolicyDescription { get; set; } = string.Empty;
        public int DaysUntilDeparture { get; set; }
        
        // Thông tin Bill
        public BillInfoDTO? BillInfo { get; set; }
        
        // Thông tin User
        public UserInfoDTO? UserInfo { get; set; }
    }

    // DTO cho thông tin Bill trong refund
    public class BillInfoDTO
    {
        public Guid BillID { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal? RefundPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // DTO cho thông tin User trong refund và schedule
    public class UserInfoDTO
    {
        public Guid UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    // DTO cho danh sách refund với phân trang
    public class RefundListResponseDTO
    {
        public List<RefundResponseDTO> Refunds { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalPages { get; set; }
    }

    // DTO cho thống kê refund
    public class RefundStatsDTO
    {
        public int TotalRefunds { get; set; }
        public int PendingRefunds { get; set; }
        public int ApprovedRefunds { get; set; }
        public int DeniedRefunds { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public decimal PendingRefundAmount { get; set; }
    }

}
