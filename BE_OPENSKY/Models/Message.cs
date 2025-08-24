using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Message
    {
        [Key]
        public int MessageID { get; set; }
        
        [Required]
        public int Sender { get; set; }
        
        [Required]
        public int Receiver { get; set; }
        
        [Required]
        public string MessageText { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual User SenderUser { get; set; } = null!;
        public virtual User ReceiverUser { get; set; } = null!;
    }
}
