using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class TourItinerary
    {
        [Key]
        public Guid ItineraryID { get; set; }
        
        [Required]
        public Guid TourID { get; set; }
        
        [Required]
        public int DayNumber { get; set; }
        
        [Required]
        public string Location { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        // Navigation properties
        public virtual Tour Tour { get; set; } = null!;
        public virtual ICollection<ScheduleItinerary> ScheduleItineraries { get; set; } = new List<ScheduleItinerary>();
    }
}
