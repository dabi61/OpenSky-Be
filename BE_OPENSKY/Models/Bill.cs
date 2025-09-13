using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Bill
    {
        [Key]
        public Guid BillID { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        public Guid BookingID { get; set; }
        
        [Required]
        public decimal Deposit { get; set; }
        
        public decimal? RefundPrice { get; set; }
        
        [Required]
        public decimal TotalPrice { get; set; }
        
        [Required]
        public BillStatus Status { get; set; } = BillStatus.Pending; // Trạng thái: Chờ(Pending), Đã thanh toán(Paid), Đã hủy(Cancelled), Đã hoàn tiền(Refunded)
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Khóa ngoại đến UserVoucher (tùy chọn)
        public Guid? UserVoucherID { get; set; }
        
        // Thuộc tính điều hướng
        public virtual User User { get; set; } = null!;
        public virtual Booking Booking { get; set; } = null!;
        public virtual UserVoucher? UserVoucher { get; set; } // Voucher được sử dụng cho hóa đơn này
        public virtual ICollection<BillDetail> BillDetails { get; set; } = new List<BillDetail>();
        public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
