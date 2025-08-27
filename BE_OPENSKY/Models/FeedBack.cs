// using directives đã đưa vào GlobalUsings

namespace BE_OPENSKY.Models
{
    public class FeedBack
    {
        [Key]
        public Guid FeedBackID { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        public TableType TableType { get; set; } // Loại đánh giá: Tour, Hotel, User
        
        [Required]
        public Guid TableID { get; set; } // ID của đối tượng được đánh giá
        
        [Required]
        [Range(1, 5)]
        public int Rate { get; set; } // Điểm đánh giá từ 1-5 sao
        
        public string? Description { get; set; } // Nội dung đánh giá
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Ngày tạo đánh giá
        
        // Thuộc tính điều hướng
        public virtual User User { get; set; } = null!; // Người đánh giá
    }
}
