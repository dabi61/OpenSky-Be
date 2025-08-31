namespace BE_OPENSKY.DTOs
{
    public class TourCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int NumberOfDays { get; set; }
        public int MaxPeople { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public int Star { get; set; } = 0;
    }

    public class TourUpdateDTO
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public int? NumberOfDays { get; set; }
        public int? MaxPeople { get; set; }
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public int? Star { get; set; }
        public TourStatus? Status { get; set; }
    }

    public class TourResponseDTO
    {
        public Guid TourID { get; set; }
        public Guid UserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int NumberOfDays { get; set; }
        public int MaxPeople { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public TourStatus Status { get; set; }
        public int Star { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserFullName { get; set; } = string.Empty;
    }

    public class TourListDTO
    {
        public Guid TourID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int NumberOfDays { get; set; }
        public int MaxPeople { get; set; }
        public decimal Price { get; set; }
        public TourStatus Status { get; set; }
        public int Star { get; set; }
        public string UserFullName { get; set; } = string.Empty;
    }
}
