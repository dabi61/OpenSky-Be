using System.ComponentModel.DataAnnotations;

namespace BE_OPENSKY.DTOs
{
    // DTO cho cập nhật trạng thái phòng
public class UpdateRoomStatusDTO
{
    [Required]
    public string Status { get; set; } = string.Empty; // Nhận string: "Available", "Occupied", "Maintenance"
}

    // DTO cho phản hồi trạng thái phòng
    public class RoomStatusResponseDTO
    {
        public Guid RoomID { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // DTO cho danh sách phòng theo trạng thái
    public class RoomStatusListDTO
    {
        public List<RoomStatusResponseDTO> Rooms { get; set; } = new();
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public int OutOfOrderRooms { get; set; }
    }
}
