namespace BE_OPENSKY.Services;

public class ImageService : IImageService
{
    private readonly ApplicationDbContext _context;
    private readonly ICloudinaryService _cloudinaryService;

    public ImageService(ApplicationDbContext context, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<List<string>> UploadMultipleImagesAsync(IEnumerable<IFormFile> files, string folder, TableTypeImage tableType, Guid typeId)
    {
        var uploadedUrls = new List<string>();

        foreach (var file in files)
        {
            try
            {
                if (file.Length > 0)
                {
                    // Upload to Cloudinary
                    var imageUrl = await _cloudinaryService.UploadImageAsync(file, folder);
                    
                    // Save to database
                    var image = new Image
                    {
                        TableType = tableType,
                        TypeID = typeId,
                        URL = imageUrl,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Images.Add(image);
                    uploadedUrls.Add(imageUrl);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue with other files
                Console.WriteLine($"Failed to upload image {file.FileName}: {ex.Message}");
            }
        }

        if (uploadedUrls.Any())
        {
            await _context.SaveChangesAsync();
        }

        return uploadedUrls;
    }

    public async Task<List<string>> GetImagesByTypeAsync(TableTypeImage tableType, Guid typeId)
    {
        return await _context.Images
            .Where(i => i.TableType == tableType && i.TypeID == typeId)
            .OrderBy(i => i.CreatedAt)
            .Select(i => i.URL)
            .ToListAsync();
    }

    public async Task<bool> DeleteImageAsync(int imageId, Guid userId)
    {
        var image = await _context.Images.FindAsync(imageId);
        if (image == null) return false;

        // Check if user owns the hotel/room that this image belongs to
        bool canDelete = false;

        if (image.TableType == TableTypeImage.Hotel)
        {
            canDelete = await _context.Hotels
                .AnyAsync(h => h.HotelID == image.TypeID && h.UserID == userId);
        }
        else if (image.TableType == TableTypeImage.RoomHotel)
        {
            canDelete = await _context.HotelRooms
                .Include(r => r.Hotel)
                .AnyAsync(r => r.RoomID == image.TypeID && r.Hotel.UserID == userId);
        }

        if (!canDelete) return false;

        _context.Images.Remove(image);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAllImagesForTypeAsync(TableTypeImage tableType, Guid typeId)
    {
        var images = await _context.Images
            .Where(i => i.TableType == tableType && i.TypeID == typeId)
            .ToListAsync();

        if (images.Any())
        {
            _context.Images.RemoveRange(images);
            await _context.SaveChangesAsync();
        }

        return true;
    }
}
