namespace BE_OPENSKY.DTOs;

// DTO tải ảnh lên
public record ImageUploadDTO
{
    public string TableType { get; init; } = string.Empty; // "Tour", "Hotel", "HotelRoom", "User"
    public int TypeID { get; init; } // ID của đối tượng (TourID, HotelID, RoomID, UserID)
    public IFormFile File { get; init; } = null!; // File ảnh upload
    public string? Description { get; init; } // Mô tả ảnh (optional)
}

// DTO phản hồi ảnh
public record ImageResponseDTO
{
    public int ImgID { get; init; } // ID ảnh
    public string TableType { get; init; } = string.Empty; // Loại đối tượng
    public int TypeID { get; init; } // ID đối tượng
    public string URL { get; init; } = string.Empty; // Link ảnh trên Cloudinary
    public string? Description { get; init; } // Mô tả ảnh
    public DateTime CreatedAt { get; init; } // Ngày tải lên
}

// DTO cập nhật ảnh
public record ImageUpdateDTO
{
    public string? Description { get; init; } // Chỉ cho phép sửa mô tả
}

// DTO danh sách ảnh theo đối tượng
public record GetImagesDTO
{
    public string TableType { get; init; } = string.Empty; // "Tour", "Hotel", "HotelRoom", "User"
    public int TypeID { get; init; } // ID của đối tượng
}

// DTO kết quả upload
public record ImageUploadResultDTO
{
    public bool Success { get; init; } // Thành công hay không
    public string Message { get; init; } = string.Empty; // Thông báo
    public ImageResponseDTO? Image { get; init; } // Thông tin ảnh nếu thành công
    public string? Error { get; init; } // Lỗi nếu có
}
