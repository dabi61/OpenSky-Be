using Microsoft.AspNetCore.Http;

namespace BE_OPENSKY.DTOs
{
    // DTO cho cập nhật thông tin khách sạn (JSON)
    public class UpdateHotelDTO
    {
        public string? HotelName { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Province { get; set; }
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public decimal? Latitude { get; set; }
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public decimal? Longitude { get; set; }
        [Range(1, 5)]
        public int? Star { get; set; }
    }

    // DTO cho cập nhật thông tin khách sạn với ảnh (multipart/form-data)
    public class UpdateHotelWithImagesDTO
    {
        [Required]
        public Guid HotelId { get; set; }
        public string? HotelName { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Province { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public int? Star { get; set; }
        public string? ImageAction { get; set; } = "keep"; // "keep", "replace"
        // Files sẽ được xử lý từ form.Files
    }

    // DTO phản hồi khi cập nhật khách sạn với ảnh
    public class UpdateHotelWithImagesResponseDTO
    {
        public string Message { get; set; } = string.Empty;
        public List<string> UploadedImageUrls { get; set; } = new();
        public List<string> FailedUploads { get; set; } = new();
        public List<string> DeletedImageUrls { get; set; } = new(); // Ảnh đã xóa
        public int SuccessImageCount { get; set; }
        public int FailedImageCount { get; set; }
        public int DeletedImageCount { get; set; }
        public string ImageAction { get; set; } = "keep"; // Hành động đã thực hiện
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
        public string Province { get; set; } = string.Empty;
        public decimal Latitude { get; set; } // Vĩ độ
        public decimal Longitude { get; set; } // Kinh độ
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
        public Guid HotelId { get; set; }
        
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

    // DTO cho tạo phòng mới với ảnh (multipart form data)
    public class CreateRoomWithImagesDTO
    {
        [Required]
        public Guid HotelId { get; set; }
        
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
        
        // Ảnh sẽ được xử lý riêng từ form files
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

    // DTO cho cập nhật thông tin phòng với ảnh (multipart/form-data)
    public class UpdateRoomWithImagesDTO
    {
        public string? RoomName { get; set; }
        public string? RoomType { get; set; }
        public string? Address { get; set; }
        public decimal? Price { get; set; }
        public int? MaxPeople { get; set; }
        public string? ImageAction { get; set; } = "keep"; // "keep", "replace"
        // Files sẽ được xử lý từ form.Files
    }

    // DTO phản hồi khi cập nhật phòng với ảnh
    public class UpdateRoomWithImagesResponseDTO
    {
        public string Message { get; set; } = string.Empty;
        public List<string> UploadedImageUrls { get; set; } = new();
        public List<string> FailedUploads { get; set; } = new();
        public List<string> DeletedImageUrls { get; set; } = new(); // Ảnh đã xóa
        public int SuccessImageCount { get; set; }
        public int FailedImageCount { get; set; }
        public int DeletedImageCount { get; set; }
        public string ImageAction { get; set; } = "keep"; // Hành động đã thực hiện
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

    // DTOs cho tìm kiếm và lọc khách sạn
    public class HotelSearchDTO
    {
        public string? Query { get; set; } // Tìm kiếm theo tên
        public string? Province { get; set; } // Lọc theo tỉnh
        public string? Address { get; set; } // Lọc theo địa chỉ
        public List<int>? Stars { get; set; } // Lọc theo số sao [4,5]
        public decimal? MinPrice { get; set; } // Giá tối thiểu
        public decimal? MaxPrice { get; set; } // Giá tối đa
        public string? SortBy { get; set; } = "name"; // Sắp xếp theo: name, price, star, createdAt
        public string? SortOrder { get; set; } = "asc"; // asc, desc
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
    }

    public class HotelSearchResultDTO
    {
        public Guid HotelID { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public decimal Latitude { get; set; } // Vĩ độ
        public decimal Longitude { get; set; } // Kinh độ
        public string? Description { get; set; }
        public int Star { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<string> Images { get; set; } = new(); // URLs của ảnh khách sạn
        public decimal MinPrice { get; set; } // Giá phòng rẻ nhất
        public decimal MaxPrice { get; set; } // Giá phòng đắt nhất
        public int TotalRooms { get; set; } // Tổng số phòng
        public int AvailableRooms { get; set; } // Số phòng còn trống
    }

    public class HotelSearchResponseDTO
    {
        public List<HotelSearchResultDTO> Hotels { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // DTO cho response tạo phòng với ảnh
    public class CreateRoomWithImagesResponseDTO
    {
        public Guid RoomID { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> UploadedImageUrls { get; set; } = new();
        public List<string> FailedUploads { get; set; } = new();
        public int SuccessImageCount { get; set; }
        public int FailedImageCount { get; set; }
    }
}
