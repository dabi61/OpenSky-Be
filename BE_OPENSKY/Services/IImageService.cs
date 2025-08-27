namespace BE_OPENSKY.Services;

// Interface Service cho Image - Xử lý logic nghiệp vụ ảnh
public interface IImageService
{
    Task<ImageResponseDTO> UploadImageAsync(ImageUploadDTO imageDto); // Tải lên ảnh
    Task<IEnumerable<ImageResponseDTO>> GetImagesByTableAsync(TableType tableType, Guid typeId); // Lấy ảnh theo đối tượng
    Task<ImageResponseDTO?> GetImageByIdAsync(int imgId); // Lấy ảnh theo ID
    // UpdateImageDescriptionAsync removed - Description property no longer exists
    Task<bool> DeleteImageAsync(int imgId); // Xóa ảnh
    Task<bool> DeleteAllImagesAsync(TableType tableType, Guid typeId); // Xóa tất cả ảnh của đối tượng
    Task<ImageResponseDTO?> GetUserAvatarAsync(Guid userId); // Lấy avatar của user
    Task<ImageResponseDTO?> SetUserAvatarAsync(Guid userId, int imgId); // Đặt ảnh làm avatar cho user
}
