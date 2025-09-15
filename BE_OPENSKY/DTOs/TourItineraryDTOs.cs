using BE_OPENSKY.Models;

namespace BE_OPENSKY.DTOs
{
    // DTO cho tạo tour itinerary mới
    public class CreateTourItineraryDTO
    {
        public Guid TourID { get; set; }
        public int DayNumber { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // DTO cho cập nhật tour itinerary
    public class UpdateTourItineraryDTO
    {
        public int? DayNumber { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }

    // DTO cho response tour itinerary
    public class TourItineraryResponseDTO
    {
        public Guid ItineraryID { get; set; }
        public Guid TourID { get; set; }
        public int DayNumber { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsDeleted { get; set; }
        public string? TourName { get; set; }
    }
}
