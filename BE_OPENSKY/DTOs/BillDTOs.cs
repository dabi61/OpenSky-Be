using System.ComponentModel.DataAnnotations;
using BE_OPENSKY.Models;

namespace BE_OPENSKY.DTOs
{

    // DTO cho Bill response
    public class BillResponseDTO
    {
        public Guid BillID { get; set; }
        public Guid UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid BookingID { get; set; }
        public decimal Deposit { get; set; }
        public decimal? RefundPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal OriginalTotalPrice { get; set; } // Tổng tiền gốc trước khi giảm giá
        public decimal DiscountAmount { get; set; } // Số tiền được giảm
        public decimal DiscountPercent { get; set; } // Phần trăm giảm giá
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid? UserVoucherID { get; set; }
        public VoucherInfoDTO? VoucherInfo { get; set; } // Thông tin voucher được sử dụng
        public List<BillDetailResponseDTO> BillDetails { get; set; } = new();
    }

    // DTO cho thông tin voucher
    public class VoucherInfoDTO
    {
        public string Code { get; set; } = string.Empty;
        public int Percent { get; set; }
        public TableType TableType { get; set; }
        public string? Description { get; set; }
    }

    // DTO cho áp dụng voucher vào bill đã có
    public class ApplyVoucherToBillDTO
    {
        public Guid UserVoucherID { get; set; }
    }

    // DTO cho response khi áp dụng voucher
    public class ApplyVoucherResponseDTO
    {
        public Guid BillID { get; set; }
        public decimal OriginalTotalPrice { get; set; }
        public decimal NewTotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public VoucherInfoDTO? VoucherInfo { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // DTO cho BillDetail response
    public class BillDetailResponseDTO
    {
        public Guid BillDetailID { get; set; }
        public Guid BillID { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public Guid ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
