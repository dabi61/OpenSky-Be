using BE_OPENSKY.Models;

namespace BE_OPENSKY.DTOs
{
    // DTO cho tạo schedule itinerary mới
    public class CreateScheduleItineraryDTO
    {
        public Guid ScheduleID { get; set; }
        public Guid ItineraryID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }

    // DTO cho cập nhật schedule itinerary
    public class UpdateScheduleItineraryDTO
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    // DTO cho response schedule itinerary
    public class ScheduleItineraryResponseDTO
    {
        public Guid ScheduleItID { get; set; }
        public Guid ScheduleID { get; set; }
        public Guid ItineraryID { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public int DayNumber { get; set; }
        public ScheduleResponseDTO? Schedule { get; set; }
        public TourItineraryResponseDTO? TourItinerary { get; set; }
    }
}
