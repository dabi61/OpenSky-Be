using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class ScheduleItinerary
    {
        [Key]
        public int ScheduleItID { get; set; }
        
        [Required]
        public int ScheduleID { get; set; }
        
        [Required]
        public int ItineraryID { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public DateTime EndTime { get; set; }
        
        // Navigation properties
        public virtual Schedule Schedule { get; set; } = null!;
        public virtual TourItinerary TourItinerary { get; set; } = null!;
    }
}
