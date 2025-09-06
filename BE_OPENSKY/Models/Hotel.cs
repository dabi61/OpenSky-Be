using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Hotel
    {
        [Key]
        public Guid HotelID { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        public string Province { get; set; } = string.Empty;
        
        public string? Coordinates { get; set; }
        
        [Required]
        public string HotelName { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public HotelStatus Status { get; set; } = HotelStatus.Active; // Active, Inactive, Draft
        
        [Required]
        public int Star { get; set; } = 0; // Rating from 1-5
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<HotelRoom> HotelRooms { get; set; } = new List<HotelRoom>();
    }
}
