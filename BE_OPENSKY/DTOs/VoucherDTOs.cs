using BE_OPENSKY.Models;

namespace BE_OPENSKY.DTOs
{
    // DTO cho tạo voucher mới
    public class CreateVoucherDTO
    {
        public string Code { get; set; } = string.Empty;
        public int Percent { get; set; }
        public TableType TableType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }
        // MaxUsage removed from request DTO (không xử lý)
    }

    // DTO cho cập nhật voucher
    public class UpdateVoucherDTO
    {
        public string? Code { get; set; }
        public int? Percent { get; set; }
        public TableType? TableType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Description { get; set; }
        // MaxUsage removed from update DTO
    }

    // DTO cho response voucher
    public class VoucherResponseDTO
    {
        public Guid VoucherID { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Percent { get; set; }
        public TableType TableType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }
        // MaxUsage removed from response DTO display
        public int UsedCount { get; set; } // Số lần đã sử dụng
        public bool IsExpired { get; set; } // Đã hết hạn chưa
        public bool IsAvailable { get; set; } // Còn sử dụng được không
    }

    // DTO cho danh sách voucher có phân trang
    public class VoucherListResponseDTO
    {
        public List<VoucherResponseDTO> Vouchers { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalPages { get; set; }
    }
}
