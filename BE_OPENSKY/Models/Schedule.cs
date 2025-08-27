using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Schedule
    {
        [Key]
        public Guid ScheduleID { get; set; }
        
        [Required]
        public Guid TourID { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        public DateTime StartTime { get; set; }
        
        [Required]
        public DateTime EndTime { get; set; }
        
        [Required]
        public int NumberPeople { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Tour Tour { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<ScheduleItinerary> ScheduleItineraries { get; set; } = new List<ScheduleItinerary>();
    }
}
