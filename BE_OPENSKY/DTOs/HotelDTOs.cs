using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.DTOs
{
    // DTO cho cập nhật thông tin khách sạn
    public class UpdateHotelDTO
    {
        public string? HotelName { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? District { get; set; }
        public string? Coordinates { get; set; }
        [Range(1, 5)]
        public int? Star { get; set; }
    }

    // DTO cho phản hồi chi tiết khách sạn
    public class HotelDetailResponseDTO
    {
        public Guid HotelID { get; set; }
        public Guid UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Address { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string? Coordinates { get; set; }
        public int Star { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Images { get; set; } = new(); // URLs của ảnh khách sạn
        public List<RoomSummaryDTO> Rooms { get; set; } = new(); // Danh sách phòng (có phân trang)
        public int TotalRooms { get; set; } // Tổng số phòng
    }

    // DTO tóm tắt phòng trong danh sách khách sạn
    public class RoomSummaryDTO
    {
        public Guid RoomID { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MaxPeople { get; set; }
        public string? FirstImage { get; set; } // Ảnh đại diện của phòng
    }

    // DTO cho tạo phòng mới
    public class CreateRoomDTO
    {
        [Required]
        public string RoomName { get; set; } = string.Empty;
        
        [Required]
        public string RoomType { get; set; } = string.Empty;
        
        [Required]
        public string Address { get; set; } = string.Empty;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phòng phải lớn hơn 0")]
        public decimal Price { get; set; }
        
        [Required]
        [Range(1, 20, ErrorMessage = "Số lượng người phải từ 1 đến 20")]
        public int MaxPeople { get; set; }
    }

    // DTO cho cập nhật thông tin phòng
    public class UpdateRoomDTO
    {
        public string? RoomName { get; set; }
        public string? RoomType { get; set; }
        public string? Address { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phòng phải lớn hơn 0")]
        public decimal? Price { get; set; }
        [Range(1, 20, ErrorMessage = "Số lượng người phải từ 1 đến 20")]
        public int? MaxPeople { get; set; }
    }

    // DTO cho phản hồi chi tiết phòng
    public class RoomDetailResponseDTO
    {
        public Guid RoomID { get; set; }
        public Guid HotelID { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MaxPeople { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Images { get; set; } = new(); // URLs của ảnh phòng
    }

    // DTO cho upload ảnh trong Swagger UI
    public class ImageUploadDTO
    {
        public IFormFile File { get; set; } = null!;
    }

    // DTO cho upload nhiều ảnh trong Swagger UI - simplified
    public class MultipleImageUploadDTO
    {
        public IFormFileCollection Files { get; set; } = new FormFileCollection();
    }

    // DTO cho upload nhiều ảnh
    public class MultipleImageUploadResponseDTO
    {
        public List<string> UploadedUrls { get; set; } = new();
        public List<string> FailedUploads { get; set; } = new();
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // DTO cho phân trang danh sách phòng
    public class PaginatedRoomsResponseDTO
    {
        public List<RoomSummaryDTO> Rooms { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRooms { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
}
