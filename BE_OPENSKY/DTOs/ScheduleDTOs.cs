using BE_OPENSKY.Models;
using System.Text.Json.Serialization;

namespace BE_OPENSKY.DTOs
{
    // DTO cho thông tin tour trong schedule response
    public class ScheduleTourInfoDTO
    {
        public Guid TourID { get; set; }
        public string TourName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxPeople { get; set; }
        public decimal Price { get; set; }
        public int Star { get; set; }
        public string? ImageUrl { get; set; }
    }

    // DTO cho tạo schedule mới
    public class CreateScheduleDTO
    {
        public Guid TourID { get; set; }
        public Guid UserID { get; set; } // ID của TourGuide được phân công
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    // DTO cho cập nhật schedule (thời gian, status và số lượng người)
    public class UpdateScheduleDTO
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ScheduleStatus? Status { get; set; }
    }

    // DTO cập nhật schedule kèm ID trong body
    public class UpdateScheduleWithIdDTO
    {
        public Guid ScheduleID { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ScheduleStatus? Status { get; set; }
    }

    // DTO cho response schedule
    public class ScheduleResponseDTO
    {
        public Guid ScheduleID { get; set; }
        public Guid TourID { get; set; }
        public Guid UserID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int NumberPeople { get; set; } // Số chỗ còn lại (remaining slots)
        public ScheduleStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TourName { get; set; } // Giữ lại để tương thích
        public UserInfoDTO? User { get; set; }
        public ScheduleTourInfoDTO? Tour { get; set; } // Thông tin tour chi tiết
    }

    // DTO cho danh sách schedule có phân trang
    public class ScheduleListResponseDTO
    {
        public List<ScheduleResponseDTO> Schedules { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int Size { get; set; }
        public int TotalPages { get; set; }
    }
}
