using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class ScheduleItinerary
    {
        [Key]
        public Guid ScheduleItID { get; set; }
        
        [Required]
        public Guid ScheduleID { get; set; }
        
        [Required]
        public Guid ItineraryID { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        public DateTime? EndTime { get; set; }
        
        // Navigation properties
        public virtual Schedule Schedule { get; set; } = null!;
        public virtual TourItinerary TourItinerary { get; set; } = null!;
    }
}
