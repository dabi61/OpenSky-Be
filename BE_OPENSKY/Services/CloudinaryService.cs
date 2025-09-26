using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace BE_OPENSKY.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudinarySettings = configuration.GetSection("Cloudinary");
        
        var account = new Account(
            cloudinarySettings["CloudName"],
            cloudinarySettings["ApiKey"],
            cloudinarySettings["ApiSecret"]
        );

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder = "avatars")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File không được để trống");

        // Kiểm tra định dạng file
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
            throw new ArgumentException("Chỉ hỗ trợ các định dạng ảnh: JPG, JPEG, PNG, GIF, WEBP");

        // Kiểm tra kích thước file (max 5MB)
        if (file.Length > 5 * 1024 * 1024)
            throw new ArgumentException("Kích thước file không được vượt quá 5MB");

        using var stream = file.OpenReadStream();
        
        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            Transformation = new Transformation()
                .Quality("auto")
                .FetchFormat("auto"),
            PublicId = $"{folder}_{Guid.NewGuid()}"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            throw new Exception($"Lỗi khi upload ảnh: {uploadResult.Error.Message}");

        return uploadResult.SecureUrl.ToString();
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return false;

        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        return result.Result == "ok";
    }
}
