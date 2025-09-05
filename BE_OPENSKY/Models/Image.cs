// using directives đã đưa vào GlobalUsings

namespace BE_OPENSKY.Models
{
    // Model Image - Quản lý ảnh trên Cloudinary
    public class Image
    {
        [Key]
        public int ImgID { get; set; } // ID ảnh
        
        [Required]
        public TableTypeImage TableType { get; set; } // Loại đối tượng: User, Hotel, RoomHotel, Tour
        
        [Required]
        public Guid TypeID { get; set; } // ID của đối tượng (TourID, HotelID, RoomID, UserID)
        
        [Required]
        public string URL { get; set; } = string.Empty; // Link ảnh trên Cloudinary
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Ngày tải lên
    }
}
