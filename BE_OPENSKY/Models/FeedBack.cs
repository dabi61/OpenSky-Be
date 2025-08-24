using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class FeedBack
    {
        [Key]
        public int FeedBackID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public string TableType { get; set; } = string.Empty; // Tour, Hotel, User
        
        [Required]
        public int TableID { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int Rate { get; set; }
        
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
    }
}
