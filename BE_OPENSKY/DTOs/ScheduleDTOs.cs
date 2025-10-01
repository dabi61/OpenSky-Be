using BE_OPENSKY.Models;
using System.Text.Json.Serialization;

namespace BE_OPENSKY.DTOs
{
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
        public int NumberPeople { get; set; }
        public ScheduleStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TourName { get; set; }
        public UserInfoDTO? User { get; set; }
        public int? RemainingSlots { get; set; } // Giữ lại để tương thích; bằng NumberPeople
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
