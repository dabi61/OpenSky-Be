namespace BE_OPENSKY.DTOs;

// DTO tải ảnh lên
public record ImageUploadDTO
{
    public TableType TableType { get; init; } // Tour, Hotel, User
    public Guid TypeID { get; init; } // ID của đối tượng (TourID, HotelID, RoomID, UserID)
    public IFormFile File { get; init; } = null!; // File ảnh upload
}

// DTO phản hồi ảnh
public record ImageResponseDTO
{
    public int ImgID { get; init; } // ID ảnh
    public TableType TableType { get; init; } // Loại đối tượng
    public Guid TypeID { get; init; } // ID đối tượng
    public string URL { get; init; } = string.Empty; // Link ảnh trên Cloudinary
    public DateTime CreatedAt { get; init; } // Ngày tải lên
}

// DTO cập nhật ảnh - removed since Description no longer exists
// public record ImageUpdateDTO

// DTO danh sách ảnh theo đối tượng
public record GetImagesDTO
{
    public TableType TableType { get; init; } // Tour, Hotel, User
    public Guid TypeID { get; init; } // ID của đối tượng
}

// DTO kết quả upload
public record ImageUploadResultDTO
{
    public bool Success { get; init; } // Thành công hay không
    public string Message { get; init; } = string.Empty; // Thông báo
    public ImageResponseDTO? Image { get; init; } // Thông tin ảnh nếu thành công
    public string? Error { get; init; } // Lỗi nếu có
}
