using System.ComponentModel.DataAnnotations;
using BE_OPENSKY.Models;

namespace BE_OPENSKY.DTOs
{
    // DTO cho tạo tour mới
    public class CreateTourDTO
    {
        [Required]
        [StringLength(200, ErrorMessage = "Tên tour không được quá 200 ký tự")]
        public string TourName { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Mô tả không được quá 2000 ký tự")]
        public string? Description { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được quá 100 ký tự")]
        public string Province { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá tour phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Số người tối đa phải từ 1 đến 100")]
        public int MaxPeople { get; set; }
    }

    // DTO cho cập nhật tour
    public class UpdateTourDTO
    {
        [StringLength(200, ErrorMessage = "Tên tour không được quá 200 ký tự")]
        public string? TourName { get; set; }

        [StringLength(2000, ErrorMessage = "Mô tả không được quá 2000 ký tự")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được quá 100 ký tự")]
        public string? Province { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá tour phải lớn hơn 0")]
        public decimal? Price { get; set; }

        [Range(1, 100, ErrorMessage = "Số người tối đa phải từ 1 đến 100")]
        public int? MaxPeople { get; set; }
    }

    // DTO cho thông tin ảnh tour
    public class TourImageDTO
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    // DTO cho response tour
    public class TourResponseDTO
    {
        public Guid TourID { get; set; }
        public Guid UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string TourName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public int Star { get; set; }
        public decimal Price { get; set; }
        public int MaxPeople { get; set; }
        public TourStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<TourImageDTO> Images { get; set; } = new();
    }

    // DTO cho tóm tắt tour (trong danh sách)
    public class TourSummaryDTO
    {
        public Guid TourID { get; set; }
        public string TourName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public int Star { get; set; }
        public decimal Price { get; set; }
        public int MaxPeople { get; set; }
        public TourStatus Status { get; set; }
        public string? FirstImage { get; set; }
    }

    // DTO cho phân trang danh sách tour
    public class PaginatedToursResponseDTO
    {
        public List<TourSummaryDTO> Tours { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalTours { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // DTO cho tìm kiếm tour
    public class TourSearchDTO
    {
        public string? Keyword { get; set; }
        public string? Province { get; set; }
        public int? Star { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public TourStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "desc";
    }

    // DTO cho response tìm kiếm tour
    public class TourSearchResponseDTO
    {
        public List<TourSummaryDTO> Tours { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    // DTO cho cập nhật trạng thái tour
    public class UpdateTourStatusDTO
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    // DTO cho tạo tour với ảnh (multipart form data)
    public class CreateTourWithImagesDTO
    {
        [Required]
        [StringLength(200, ErrorMessage = "Tên tour không được quá 200 ký tự")]
        public string TourName { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Mô tả không được quá 2000 ký tự")]
        public string? Description { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được quá 100 ký tự")]
        public string Province { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá tour phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Số người tối đa phải từ 1 đến 100")]
        public int MaxPeople { get; set; }

        // Files sẽ được xử lý từ form.Files
    }

    // DTO phản hồi khi tạo tour với ảnh
    public class CreateTourWithImagesResponseDTO
    {
        public Guid TourID { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> UploadedImageUrls { get; set; } = new();
        public List<string> FailedUploads { get; set; } = new();
        public int SuccessImageCount { get; set; }
        public int FailedImageCount { get; set; }
    }


    // DTO mới cho cập nhật ảnh tour theo logic mới
    public class TourImageUpdateDTO
    {
        [Required]
        public Guid TourId { get; set; }
        
        [StringLength(200, ErrorMessage = "Tên tour không được quá 200 ký tự")]
        public string? TourName { get; set; }
        
        [StringLength(2000, ErrorMessage = "Mô tả không được quá 2000 ký tự")]
        public string? Description { get; set; }
        
        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string? Address { get; set; }
        
        [StringLength(100, ErrorMessage = "Tỉnh/Thành phố không được quá 100 ký tự")]
        public string? Province { get; set; }
        
        public decimal? Price { get; set; }
        public int? MaxPeople { get; set; }
        
        // ExistingImages: Giữ nguyên các ảnh không muốn xóa (IDs của ảnh hiện tại)
        public List<int>? ExistingImageIds { get; set; } = new();
        
        // NewImages: Thêm ảnh mới (sẽ được xử lý từ form.Files)
        // DeleteImages: Xóa ảnh (IDs của ảnh cần xóa)
        public List<int>? DeleteImageIds { get; set; } = new();
    }

    // DTO phản hồi cho cập nhật ảnh tour theo logic mới
    public class TourImageUpdateResponseDTO
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