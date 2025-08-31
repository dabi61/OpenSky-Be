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
        public TableType TableType { get; set; } // Tour, Hotel, Schedule
        
        [Required]
        public Guid TypeID { get; set; }
        
        [Required]
        public decimal Deposit { get; set; }
        
        public decimal? RefundPrice { get; set; }
        
        [Required]
        public decimal TotalPrice { get; set; }
        
        [Required]
        public BillStatus Status { get; set; } = BillStatus.Pending; // Pending, Paid, Cancelled, Refunded
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Foreign key đến UserVoucher (optional)
        public Guid? UserVoucherID { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual UserVoucher? UserVoucher { get; set; } // Voucher được sử dụng cho bill này
        public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
