namespace BE_OPENSKY.Services;

public interface IImageService
{
    Task<List<string>> UploadMultipleImagesAsync(IEnumerable<IFormFile> files, string folder, TableTypeImage tableType, Guid typeId);
    Task<List<string>> GetImagesByTypeAsync(TableTypeImage tableType, Guid typeId);
    Task<bool> DeleteImageAsync(int imageId, Guid userId);
    Task<bool> DeleteAllImagesForTypeAsync(TableTypeImage tableType, Guid typeId);
}
