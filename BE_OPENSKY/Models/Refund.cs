using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Refund
    {
        [Key]
        public Guid RefundID { get; set; }
        
        [Required]
        public Guid BillID { get; set; }
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Bill Bill { get; set; } = null!;
    }
}
