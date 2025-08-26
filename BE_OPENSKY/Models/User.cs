using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PassWord { get; set; } = string.Empty;
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        public string? ProviderId { get; set; }
        
        [Required]
        public string Role { get; set; } = RoleConstants.Customer; // Supervisor, TourGuide, Admin, Customer, Hotel
        
        public string? NumberPhone { get; set; }
        
        public string? CitizenId { get; set; }
        
        public DateTime? DoB { get; set; }
        
        public string? AvatarURL { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
        public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
        public virtual ICollection<FeedBack> FeedBacks { get; set; } = new List<FeedBack>();
        public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
        public virtual ICollection<Tour> Tours { get; set; } = new List<Tour>();
        public virtual ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();

    }
}
