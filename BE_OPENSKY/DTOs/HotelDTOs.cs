namespace BE_OPENSKY.DTOs
{
    public class HotelCreateDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string? Coordinates { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Star { get; set; } = 0;
    }

    public class HotelUpdateDTO
    {
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? District { get; set; }
        public string? Coordinates { get; set; }
        public string? HotelName { get; set; }
        public string? Description { get; set; }
        public int? Star { get; set; }
        public string? Status { get; set; }
    }

    public class HotelResponseDTO
    {
        public int HotelID { get; set; }
        public int UserID { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string? Coordinates { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public int Star { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserFullName { get; set; } = string.Empty;
    }

    public class HotelListDTO
    {
        public int HotelID { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Star { get; set; }
        public string UserFullName { get; set; } = string.Empty;
    }
}
