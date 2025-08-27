namespace BE_OPENSKY.Services;

// Service quản lý ảnh với Cloudinary
public class ImageService : IImageService
{
    private readonly ApplicationDbContext _context;
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<ImageService> _logger;

    // Các định dạng ảnh được phép
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public ImageService(
        ApplicationDbContext context,
        Cloudinary cloudinary,
        ILogger<ImageService> logger)
    {
        _context = context;
        _cloudinary = cloudinary;
        _logger = logger;
    }

    public async Task<ImageResponseDTO> UploadImageAsync(ImageUploadDTO imageDto)
    {
        try
        {
            // Validate file
            if (imageDto.File == null || imageDto.File.Length == 0)
                throw new ArgumentException("File không được để trống");

            if (imageDto.File.Length > MaxFileSize)
                throw new ArgumentException($"File quá lớn. Tối đa {MaxFileSize / (1024 * 1024)}MB");

            var extension = Path.GetExtension(imageDto.File.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                throw new ArgumentException($"Định dạng file không được hỗ trợ. Chỉ chấp nhận: {string.Join(", ", _allowedExtensions)}");

            // Validate TableType
            if (!Enum.IsDefined(typeof(TableType), imageDto.TableType))
                throw new ArgumentException("TableType không hợp lệ");

            // Upload lên Cloudinary
            using var stream = imageDto.File.OpenReadStream();
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(imageDto.File.FileName, stream),
                Folder = $"opensky/{imageDto.TableType.ToString().ToLower()}",
                PublicId = $"{imageDto.TableType}_{imageDto.TypeID}_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError($"Cloudinary upload error: {uploadResult.Error.Message}");
                throw new Exception($"Lỗi upload ảnh: {uploadResult.Error.Message}");
            }

            // Lưu vào database
            var image = new Image
            {
                TableType = imageDto.TableType,
                TypeID = imageDto.TypeID,
                URL = uploadResult.SecureUrl.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            return new ImageResponseDTO
            {
                ImgID = image.ImgID,
                URL = image.URL,
                TableType = image.TableType,
                TypeID = image.TypeID,
                CreatedAt = image.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi upload ảnh");
            throw;
        }
    }

    public async Task<IEnumerable<ImageResponseDTO>> GetImagesByTableAsync(TableType tableType, Guid typeId)
    {
        var images = await _context.Images
            .Where(i => i.TableType == tableType && i.TypeID == typeId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return images.Select(i => new ImageResponseDTO
        {
            ImgID = i.ImgID,
            URL = i.URL,
            TableType = i.TableType,
            TypeID = i.TypeID,
            CreatedAt = i.CreatedAt
        });
    }

    public async Task<ImageResponseDTO?> GetImageByIdAsync(int imgId)
    {
        var image = await _context.Images.FindAsync(imgId);
        if (image == null)
            return null;

        return new ImageResponseDTO
        {
            ImgID = image.ImgID,
            URL = image.URL,
            TableType = image.TableType,
            TypeID = image.TypeID,
            CreatedAt = image.CreatedAt
        };
    }

    // Method removed since Description property no longer exists
    // public async Task<ImageResponseDTO?> UpdateImageDescriptionAsync(int imgId, ImageUpdateDTO updateDto)

    public async Task<bool> DeleteImageAsync(int imgId)
    {
        try
        {
            var image = await _context.Images.FindAsync(imgId);
            if (image == null)
                return false;

            // Xóa trên Cloudinary
            var publicId = ExtractPublicIdFromUrl(image.URL);
            if (!string.IsNullOrEmpty(publicId))
            {
                var deleteParams = new DeletionParams(publicId);
                var deleteResult = await _cloudinary.DestroyAsync(deleteParams);
                
                if (deleteResult.Error != null)
                {
                    _logger.LogWarning($"Không thể xóa ảnh trên Cloudinary: {deleteResult.Error.Message}");
                }
            }

            // Xóa trong database
            _context.Images.Remove(image);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi xóa ảnh ID {imgId}");
            return false;
        }
    }

    public async Task<bool> DeleteAllImagesAsync(TableType tableType, Guid typeId)
    {
        try
        {
            var images = await _context.Images
                .Where(i => i.TableType == tableType && i.TypeID == typeId)
                .ToListAsync();

            if (!images.Any())
                return true; // Không có ảnh nào để xóa

            // Xóa trên Cloudinary
            foreach (var image in images)
            {
                var publicId = ExtractPublicIdFromUrl(image.URL);
                if (!string.IsNullOrEmpty(publicId))
                {
                    var deleteParams = new DeletionParams(publicId);
                    var deleteResult = await _cloudinary.DestroyAsync(deleteParams);
                    
                    if (deleteResult.Error != null)
                    {
                        _logger.LogWarning($"Không thể xóa ảnh trên Cloudinary: {deleteResult.Error.Message}");
                    }
                }
            }

            // Xóa trong database
            _context.Images.RemoveRange(images);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Lỗi khi xóa tất cả ảnh của {tableType} ID {typeId}");
            return false;
        }
    }

    public async Task<ImageResponseDTO?> GetUserAvatarAsync(Guid userId)
    {
        // Ưu tiên ảnh trong Image table
        var userImage = await _context.Images
            .Where(i => i.TableType == TableType.User && i.TypeID == userId)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        if (userImage != null)
        {
            return new ImageResponseDTO
            {
                ImgID = userImage.ImgID,
                URL = userImage.URL,
                TableType = userImage.TableType,
                TypeID = userImage.TypeID,
                CreatedAt = userImage.CreatedAt
            };
        }

        // Fallback sang User.AvatarURL
        var user = await _context.Users.FindAsync(userId);
        if (user != null && !string.IsNullOrEmpty(user.AvatarURL))
        {
            return new ImageResponseDTO
            {
                ImgID = 0, // Không có ID vì không lưu trong Image table
                URL = user.AvatarURL,
                TableType = TableType.User,
                TypeID = userId,
                CreatedAt = DateTime.UtcNow
            };
        }

        return null;
    }

    public async Task<ImageResponseDTO?> SetUserAvatarAsync(Guid userId, int imgId)
    {
        var image = await _context.Images.FindAsync(imgId);
        if (image == null || image.TableType != TableType.User || image.TypeID != userId)
            return null;

        // Cập nhật User.AvatarURL để sync
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.AvatarURL = image.URL;
            await _context.SaveChangesAsync();
        }

        return new ImageResponseDTO
        {
            ImgID = image.ImgID,
            URL = image.URL,
            TableType = image.TableType,
            TypeID = image.TypeID,
            CreatedAt = image.CreatedAt
        };
    }

    // Helper method để extract public ID từ Cloudinary URL
    private string? ExtractPublicIdFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');
            
            // Cloudinary URL format: /image/upload/v{version}/{folder}/{publicId}.{extension}
            if (segments.Length >= 2)
            {
                var fileNameWithExtension = segments[^1]; // Phần tử cuối cùng
                var fileName = Path.GetFileNameWithoutExtension(fileNameWithExtension);
                
                // Nếu có folder, combine lại
                if (segments.Length > 4 && segments[^2] != "upload")
                {
                    var folder = string.Join("/", segments.Skip(4).Take(segments.Length - 5));
                    return $"{folder}/{fileName}";
                }
                
                return fileName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Không thể extract public ID từ URL {url}: {ex.Message}");
        }
        
        return null;
    }
}