using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Tour
    {
        [Key]
        public Guid TourID { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        [StringLength(200)]
        public string TourName { get; set; } = string.Empty; // Tên tour
        
        [StringLength(2000)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Province { get; set; } = string.Empty;
        
        [Required]
        [Range(0, 5)]
        public int Star { get; set; } = 0; // Rating from 0-5, 0 = chưa có đánh giá
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; } // Giá tour
        
        [Required]
        [Range(1, 100)]
        public int MaxPeople { get; set; }
        
        [Required]
        public TourStatus Status { get; set; } = TourStatus.Active; // Active, Suspend, Removed
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public virtual ICollection<TourItinerary> TourItineraries { get; set; } = new List<TourItinerary>();
    }
}
