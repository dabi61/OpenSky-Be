using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class HotelRoom
    {
        [Key]
        public int RoomID { get; set; }
        
        [Required]
        public int HotelID { get; set; }
        
        [Required]
        public string RoomName { get; set; } = string.Empty;
        
        [Required]
        public string RoomType { get; set; } = string.Empty; // Single, Double, Suite, etc.
        
        [Required]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        public decimal Price { get; set; }
        
        [Required]
        public int MaxPeople { get; set; }
        
        // Navigation properties
        public virtual Hotel Hotel { get; set; } = null!;
    }
}
