namespace BE_OPENSKY.DTOs;

// DTO cho tạo đánh giá Hotel
public class CreateHotelReviewDTO
{
    [Required]
    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
    public int Rate { get; set; }

    [MaxLength(1000, ErrorMessage = "Nội dung đánh giá không được quá 1000 ký tự")]
    public string? Description { get; set; }
}

// DTO cho cập nhật đánh giá Hotel
public class UpdateHotelReviewDTO
{
    [Required]
    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
    public int Rate { get; set; }

    [MaxLength(1000, ErrorMessage = "Nội dung đánh giá không được quá 1000 ký tự")]
    public string? Description { get; set; }
}

// DTO cho response đánh giá Hotel
public class HotelReviewResponseDTO
{
    public Guid FeedBackID { get; set; }
    public Guid UserID { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public Guid HotelID { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public int Rate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

// DTO cho thống kê đánh giá Hotel
public class HotelReviewStatsDTO
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int Rating1Count { get; set; }
    public int Rating2Count { get; set; }
    public int Rating3Count { get; set; }
    public int Rating4Count { get; set; }
    public int Rating5Count { get; set; }
}

// DTO cho kiểm tra điều kiện đánh giá
public class ReviewEligibilityDTO
{
    public bool CanReview { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool HasExistingReview { get; set; }
    public bool HasValidBooking { get; set; }
    public bool HasPaidBill { get; set; }
    public DateTime? LastBookingDate { get; set; }
}

// DTO cho danh sách đánh giá Hotel có phân trang
public class PaginatedHotelReviewsResponseDTO
{
    public List<HotelReviewResponseDTO> Reviews { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int TotalPages { get; set; }
    public HotelReviewStatsDTO Stats { get; set; } = new();
}
