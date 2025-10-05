using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.DTOs;

// DTO cho tạo đánh giá Tour
public class CreateTourReviewDTO
{
    [Required]
    public Guid TourId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
    public int Rate { get; set; }

    [MaxLength(1000, ErrorMessage = "Nội dung đánh giá không được quá 1000 ký tự")]
    public string? Description { get; set; }
}

// DTO cho cập nhật đánh giá Tour
public class UpdateTourReviewDTO
{
    [Required]
    public Guid TourId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
    public int Rate { get; set; }

    [MaxLength(1000, ErrorMessage = "Nội dung đánh giá không được quá 1000 ký tự")]
    public string? Description { get; set; }
}

// DTO cho response đánh giá Tour
public class TourReviewResponseDTO
{
    public Guid FeedBackID { get; set; }
    public Guid UserID { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public Guid TourID { get; set; }
    public string TourName { get; set; } = string.Empty;
    public int Rate { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

// DTO cho thống kê đánh giá Tour
public class TourReviewStatsDTO
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int Rating1Count { get; set; }
    public int Rating2Count { get; set; }
    public int Rating3Count { get; set; }
    public int Rating4Count { get; set; }
    public int Rating5Count { get; set; }
}

// DTO cho kiểm tra điều kiện đánh giá Tour
public class TourReviewEligibilityDTO
{
    public bool CanReview { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool HasExistingReview { get; set; }
    public bool HasValidBooking { get; set; }
    public bool HasPaidBill { get; set; }
    public DateTime? LastBookingDate { get; set; }
}

// DTO cho danh sách đánh giá Tour có phân trang
public class PaginatedTourReviewsResponseDTO
{
    public List<TourReviewResponseDTO> Reviews { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public int TotalPages { get; set; }
    public TourReviewStatsDTO Stats { get; set; } = new();
}
