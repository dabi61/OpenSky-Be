using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Bill
    {
        [Key]
        public int BillID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public string TableType { get; set; } = string.Empty; // Tour, Hotel, Schedule
        
        [Required]
        public int TypeID { get; set; }
        
        [Required]
        public decimal Deposit { get; set; }
        
        public decimal? RefundPrice { get; set; }
        
        [Required]
        public decimal TotalPrice { get; set; }
        
        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Cancelled, Refunded
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
