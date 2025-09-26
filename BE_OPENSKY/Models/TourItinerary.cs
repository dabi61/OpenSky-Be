using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class TourItinerary
    {
        [Key]
        public Guid ItineraryID { get; set; }
        
        public Guid TourID { get; set; }
        
        [Required]
        public int DayNumber { get; set; }
        
        [Required]
        public string Location { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public bool IsDeleted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Tour Tour { get; set; } = null!;
        public virtual ICollection<ScheduleItinerary> ScheduleItineraries { get; set; } = new List<ScheduleItinerary>();
    }
}
