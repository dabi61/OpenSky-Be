using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Tour
    {
        [Key]
        public int TourID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        public int NumberOfDays { get; set; }
        
        [Required]
        public int MaxPeople { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public string Status { get; set; } = "Active"; // Active, Inactive, Draft
        
        [Required]
        public int Star { get; set; } = 0; // Rating from 1-5
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<TourItinerary> TourItineraries { get; set; } = new List<TourItinerary>();
    }
}
