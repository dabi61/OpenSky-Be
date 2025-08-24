using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Image
    {
        [Key]
        public int ImgID { get; set; }
        
        [Required]
        public string TableType { get; set; } = string.Empty; // Tour, Hotel, User
        
        [Required]
        public int TypeID { get; set; }
        
        [Required]
        public string URL { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
