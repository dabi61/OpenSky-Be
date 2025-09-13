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
        
        // Hotel booking fields
        public Guid? HotelID { get; set; }
        
        // Tour booking fields (sẽ dùng sau)
        public Guid? TourID { get; set; }
        
        // Common booking fields
        [Required]
        public DateTime CheckInDate { get; set; }
        
        [Required]
        public DateTime CheckOutDate { get; set; }
        
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
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Hotel? Hotel { get; set; }
        public virtual Bill? Bill { get; set; }
        // Tour navigation properties sẽ thêm sau
        // public virtual Tour? Tour { get; set; }
        // public virtual Schedule? Schedule { get; set; }
    }
}
