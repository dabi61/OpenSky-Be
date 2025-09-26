using Microsoft.AspNetCore.Http;

namespace BE_OPENSKY.DTOs
{
    // DTO cho cập nhật thông tin khách sạn (JSON)
    public class UpdateHotelDTO
    {
        [StringLength(200, ErrorMessage = "Tên khách sạn không được quá 200 ký tự")]
        public string? HotelName { get; set; }
        
        [StringLength(2000, ErrorMessage = "Mô tả không được quá 2000 ký tự")]
        public string? Description { get; set; }
        
        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string? Address { get; set; }
        
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được quá 100 ký tự")]
        public string? Province { get; set; }
        
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public decimal? Latitude { get; set; }
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public decimal? Longitude { get; set; }
    }

    // DTO cho phản hồi chi tiết khách sạn
    public class HotelDetailResponseDTO
    {
        public Guid HotelID { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public decimal Latitude { get; set; } // Vĩ độ
        public decimal Longitude { get; set; } // Kinh độ
        public int Star { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<HotelImageDTO> Images { get; set; } = new(); // Ảnh khách sạn với ID và URL
        public UserSummaryDTO User { get; set; } = new(); // Thông tin user đầy đủ
    }

    // DTO tóm tắt phòng trong danh sách khách sạn
    public class RoomSummaryDTO
    {
        public Guid RoomID { get; set; }
        public Guid HotelID { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int MaxPeople { get; set; }
        public string Status { get; set; } = string.Empty; // Trạng thái phòng
        public DateTime CreatedAt { get; set; }
        public string? FirstImage { get; set; } // Ảnh đại diện của phòng
    }

    // DTO cho tạo phòng mới
    public class CreateRoomDTO
    {
        [Required]
        public Guid HotelId { get; set; }
        
        [Required]
        [StringLength(200, ErrorMessage = "Tên phòng không được quá 200 ký tự")]
        public string RoomName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, ErrorMessage = "Loại phòng không được quá 100 ký tự")]
        public string RoomType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500, ErrorMessage = "Địa chỉ phòng không được quá 500 ký tự")]
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
        [StringLength(200, ErrorMessage = "Tên phòng không được quá 200 ký tự")]
        public string RoomName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100, ErrorMessage = "Loại phòng không được quá 100 ký tự")]
        public string RoomType { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500, ErrorMessage = "Địa chỉ phòng không được quá 500 ký tự")]
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
        [StringLength(200, ErrorMessage = "Tên phòng không được quá 200 ký tự")]
        public string? RoomName { get; set; }
        
        [StringLength(100, ErrorMessage = "Loại phòng không được quá 100 ký tự")]
        public string? RoomType { get; set; }
        
        [StringLength(500, ErrorMessage = "Địa chỉ phòng không được quá 500 ký tự")]
        public string? Address { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phòng phải lớn hơn 0")]
        public decimal? Price { get; set; }
        [Range(1, 20, ErrorMessage = "Số lượng người phải từ 1 đến 20")]
        public int? MaxPeople { get; set; }
    }


    // DTO cho thông tin ảnh phòng
    public class RoomImageDTO
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
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
        public string Status { get; set; } = string.Empty; // Trạng thái phòng
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<RoomImageDTO> Images { get; set; } = new(); // Ảnh phòng với ID và URL
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

    // DTO cho thông tin user (không bao gồm password)
    public class UserSummaryDTO
    {
        public Guid UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? CitizenId { get; set; }
        public DateOnly? dob { get; set; }
        public string? AvatarURL { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO tóm tắt khách sạn trong danh sách (cho admin/supervisor)
    public class HotelSummaryDTO
    {
        public Guid HotelID { get; set; }
        public Guid UserID { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public int Star { get; set; }
        public HotelStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? FirstImage { get; set; }
        public UserSummaryDTO User { get; set; } = new(); // Thông tin user đầy đủ
    }

    // DTO cho phân trang danh sách khách sạn
    public class PaginatedHotelsResponseDTO
    {
        public List<HotelSummaryDTO> Hotels { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalHotels { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // DTO cho cập nhật trạng thái khách sạn
    public class UpdateHotelStatusDTO
    {
        [Required]
        public string Status { get; set; } = string.Empty;
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

    // DTO cho cập nhật phòng với roomId trong body (JSON)
    public class UpdateRoomWithIdDTO
    {
        [Required]
        public Guid RoomId { get; set; }

        [StringLength(200, ErrorMessage = "Tên phòng không được quá 200 ký tự")]
        public string? RoomName { get; set; }

        [StringLength(100, ErrorMessage = "Loại phòng không được quá 100 ký tự")]
        public string? RoomType { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string? Address { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phòng phải lớn hơn 0")]
        public decimal? Price { get; set; }

        [Range(1, 20, ErrorMessage = "Số lượng người phải từ 1 đến 20")]
        public int? MaxPeople { get; set; }
    }

    // DTO cho xử lý ảnh theo cách mới (ExistingImages, NewImages, DeleteImages)
    public class HotelImageUpdateDTO
    {
        [Required]
        public Guid HotelId { get; set; }
        
        [StringLength(200, ErrorMessage = "Tên khách sạn không được quá 200 ký tự")]
        public string? HotelName { get; set; }
        
        [StringLength(2000, ErrorMessage = "Mô tả không được quá 2000 ký tự")]
        public string? Description { get; set; }
        
        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string? Address { get; set; }
        
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được quá 100 ký tự")]
        public string? Province { get; set; }
        
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        
        // ExistingImages: Giữ nguyên các ảnh không muốn xóa (IDs của ảnh hiện tại)
        public List<int>? ExistingImageIds { get; set; } = new();
        
        // NewImages: Thêm ảnh mới (sẽ được xử lý từ form.Files)
        // DeleteImages: Xóa ảnh (IDs của ảnh cần xóa)
        public List<int>? DeleteImageIds { get; set; } = new();
    }

    // DTO phản hồi cho việc cập nhật ảnh theo cách mới
    public class HotelImageUpdateResponseDTO
    {
        public string Message { get; set; } = string.Empty;
        public List<string> ExistingImageUrls { get; set; } = new(); // Ảnh được giữ lại
        public List<string> NewImageUrls { get; set; } = new(); // Ảnh mới được thêm
        public List<string> DeletedImageUrls { get; set; } = new(); // Ảnh đã xóa
        public List<string> FailedUploads { get; set; } = new(); // Ảnh upload thất bại
        public int ExistingImageCount { get; set; }
        public int NewImageCount { get; set; }
        public int DeletedImageCount { get; set; }
        public int FailedImageCount { get; set; }
        public int TotalImageCount { get; set; } // Tổng số ảnh sau khi cập nhật
    }

    // DTO tương tự cho Room
    public class RoomImageUpdateDTO
    {
        [Required]
        public Guid RoomId { get; set; }
        
        [StringLength(200, ErrorMessage = "Tên phòng không được quá 200 ký tự")]
        public string? RoomName { get; set; }
        
        [StringLength(100, ErrorMessage = "Loại phòng không được quá 100 ký tự")]
        public string? RoomType { get; set; }
        
        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string? Address { get; set; }
        
        public decimal? Price { get; set; }
        public int? MaxPeople { get; set; }
        
        // ExistingImages: Giữ nguyên các ảnh không muốn xóa
        public List<int>? ExistingImageIds { get; set; } = new();
        
        // DeleteImages: Xóa ảnh
        public List<int>? DeleteImageIds { get; set; } = new();
    }

    // DTO phản hồi cho việc cập nhật ảnh phòng theo cách mới
    public class RoomImageUpdateResponseDTO
    {
        public string Message { get; set; } = string.Empty;
        public List<string> ExistingImageUrls { get; set; } = new();
        public List<string> NewImageUrls { get; set; } = new();
        public List<string> DeletedImageUrls { get; set; } = new();
        public List<string> FailedUploads { get; set; } = new();
        public int ExistingImageCount { get; set; }
        public int NewImageCount { get; set; }
        public int DeletedImageCount { get; set; }
        public int FailedImageCount { get; set; }
        public int TotalImageCount { get; set; }
        
        // Properties cho backward compatibility với code cũ
        public List<string> UploadedImageUrls { get; set; } = new();
        public int SuccessImageCount { get; set; }
        public string ImageAction { get; set; } = "keep";
    }
}
