using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_OPENSKY.Models
{
    public class Booking
    {
        [Key]
        public Guid BookingID { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        public string BookingType { get; set; } = string.Empty; // "Hotel", "Tour"
        
        // Hotel booking fields
        public Guid? HotelID { get; set; }
        public Guid? RoomID { get; set; }
        
        // Tour booking fields (sẽ dùng sau)
        public Guid? TourID { get; set; }
        public Guid? ScheduleID { get; set; }
        
        // Common booking fields
        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }
        
        [Required]
        public decimal TotalPrice { get; set; }
        
        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        // Payment information
        [StringLength(50)]
        public string? PaymentMethod { get; set; }
        
        [StringLength(100)]
        public string? PaymentStatus { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Liên kết với Bill
        public Guid? BillID { get; set; }
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Hotel? Hotel { get; set; }
        public virtual HotelRoom? Room { get; set; }
        public virtual Bill? Bill { get; set; }
        // Tour navigation properties sẽ thêm sau
        // public virtual Tour? Tour { get; set; }
        // public virtual Schedule? Schedule { get; set; }
    }
}
