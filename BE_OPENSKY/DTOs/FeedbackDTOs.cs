using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.DTOs
{
    // DTO chung cho tạo feedback (Hotel hoặc Tour)
    public class CreateFeedbackDTO
    {
        [Required(ErrorMessage = "Trường 'type' là bắt buộc. Giá trị hợp lệ: 'Hotel' hoặc 'Tour'")]
        [RegularExpression("^(Hotel|Tour)$", ErrorMessage = "Giá trị 'type' không hợp lệ. Chỉ chấp nhận 'Hotel' hoặc 'Tour'")]
        public string Type { get; set; } = string.Empty; // "Hotel" hoặc "Tour"

        [Required(ErrorMessage = "Trường 'targetId' là bắt buộc")]
        public Guid TargetId { get; set; } // ID của Hotel hoặc Tour

        [Required(ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        public int Rate { get; set; }

        [MaxLength(1000, ErrorMessage = "Nội dung đánh giá không được quá 1000 ký tự")]
        public string? Description { get; set; }
    }

    // DTO chung cho cập nhật feedback
    public class UpdateFeedbackDTO
    {
        [Required(ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        public int Rate { get; set; }

        [MaxLength(1000, ErrorMessage = "Nội dung đánh giá không được quá 1000 ký tự")]
        public string? Description { get; set; }
    }
}
