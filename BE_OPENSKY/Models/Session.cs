using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.Models
{
    public class Session
    {
        [Key]
        public Guid SessionID { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        [StringLength(500)]
        public string RefreshToken { get; set; } = string.Empty;
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public DateTime ExpiresAt { get; set; } // Thời gian hết hạn refresh token (30 ngày)
        
        public DateTime? LastUsedAt { get; set; } // Lần cuối sử dụng refresh token
        
        [Required]
        public bool IsActive { get; set; } = true; // Trạng thái hoạt động, có thể thu hồi phiên
        
        // Thuộc tính điều hướng
        public virtual User User { get; set; } = null!;
    }
}
