using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class BillDetail
    {
        [Key]
        public Guid BillDetailID { get; set; }
        
        [Required]
        public Guid BillID { get; set; }
        
        [Required]
        public TableType ItemType { get; set; } // Loại dịch vụ: Tour, Hotel
        
        [Required]
        public Guid ItemID { get; set; } // ID của Tour hoặc HotelRoom
        
        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty; // Tên Tour hoặc Room để lưu trữ
        
        [Required]
        public int Quantity { get; set; } = 1; // Số lượng (số người cho Tour, số đêm cho Hotel)
        
        [Required]
        public decimal UnitPrice { get; set; } // Giá đơn vị
        
        [Required]
        public decimal TotalPrice { get; set; } // Tổng tiền = Số lượng × Giá đơn vị
        
        [StringLength(500)]
        public string? Notes { get; set; } // Ghi chú bổ sung
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Thuộc tính điều hướng
        public virtual Bill Bill { get; set; } = null!;
    }
}
