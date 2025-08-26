// using directives đã đưa vào GlobalUsings

namespace BE_OPENSKY.Endpoints;

// Endpoints cho Image - API quản lý ảnh với Cloudinary
public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        var imageGroup = app.MapGroup("/api/images")
            .WithTags("Images");

        // ===== ENDPOINTS UPLOAD ẢNH =====

        // Upload ảnh cho đối tượng (Tour, Hotel, HotelRoom, User)
        imageGroup.MapPost("/upload", async (
            [FromForm] string tableType,
            [FromForm] int typeId,
            [FromForm] IFormFile file,
            [FromForm] string? description,
            IImageService imageService,
            HttpContext context) =>
        {
            // Validate input
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { message = "File không được để trống" });

            if (string.IsNullOrEmpty(tableType))
                return Results.BadRequest(new { message = "TableType không được để trống" });

            if (!IsValidTableType(tableType))
                return Results.BadRequest(new { message = "TableType không hợp lệ. Chỉ chấp nhận: Tour, Hotel, HotelRoom, User" });

            if (typeId <= 0)
                return Results.BadRequest(new { message = "TypeID phải lớn hơn 0" });

            // Kiểm tra quyền upload
            var hasPermission = await CheckUploadPermissionAsync(tableType, typeId, context);
            if (!hasPermission)
                return Results.Forbid();

            try
            {
                var uploadDto = new ImageUploadDTO
                {
                    TableType = tableType,
                    TypeID = typeId,
                    File = file,
                    Description = description
                };

                var result = await imageService.UploadImageAsync(uploadDto);
                return Results.Created($"/api/images/{result.ImgID}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi upload ảnh",
                    detail: ex.Message,
                    statusCode: 500
                );
            }
        })
        .WithName("UploadImage")
        .WithSummary("Upload ảnh")
        .WithDescription("Upload ảnh cho Tour, Hotel, HotelRoom hoặc User")

        .Produces<ImageResponseDTO>(201)
        .Produces(400)
        .Produces(403)
        .RequireAuthorization("AuthenticatedOnly")
        .DisableAntiforgery(); // Cho phép upload file

        // ===== ENDPOINTS XEM ẢNH =====

        // Lấy danh sách ảnh theo đối tượng
        imageGroup.MapGet("/{tableType}/{typeId:int}", async (
            string tableType, 
            int typeId, 
            IImageService imageService) =>
        {
            if (!IsValidTableType(tableType))
                return Results.BadRequest(new { message = "TableType không hợp lệ" });

            var images = await imageService.GetImagesByTableAsync(tableType, typeId);
            return Results.Ok(images);
        })
        .WithName("GetImagesByObject")
        .WithSummary("Lấy ảnh theo đối tượng")
        .WithDescription("Lấy danh sách ảnh của Tour, Hotel, HotelRoom hoặc User")
        .Produces<IEnumerable<ImageResponseDTO>>();

        // Lấy ảnh theo ID
        imageGroup.MapGet("/{id:int}", async (int id, IImageService imageService) =>
        {
            var image = await imageService.GetImageByIdAsync(id);
            return image != null ? Results.Ok(image) : Results.NotFound();
        })
        .WithName("GetImageById")
        .WithSummary("Lấy ảnh theo ID")
        .WithDescription("Lấy thông tin chi tiết ảnh theo ID")
        .Produces<ImageResponseDTO>()
        .Produces(404);

        // ===== ENDPOINTS QUẢN LÝ ẢNH =====

        // Cập nhật mô tả ảnh
        imageGroup.MapPut("/{id:int}", async (
            int id, 
            ImageUpdateDTO updateDto, 
            IImageService imageService,
            HttpContext context) =>
        {
            // Kiểm tra quyền sửa
            var hasPermission = await CheckImagePermissionAsync(id, context, imageService);
            if (!hasPermission)
                return Results.Forbid();

            var image = await imageService.UpdateImageDescriptionAsync(id, updateDto);
            return image != null ? Results.Ok(image) : Results.NotFound();
        })
        .WithName("UpdateImage")
        .WithSummary("Cập nhật ảnh")
        .WithDescription("Chỉnh sửa mô tả ảnh")
        .Produces<ImageResponseDTO>()
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("AuthenticatedOnly");

        // Xóa ảnh
        imageGroup.MapDelete("/{id:int}", async (
            int id, 
            IImageService imageService,
            HttpContext context) =>
        {
            // Kiểm tra quyền xóa
            var hasPermission = await CheckImagePermissionAsync(id, context, imageService);
            if (!hasPermission)
                return Results.Forbid();

            var result = await imageService.DeleteImageAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteImage")
        .WithSummary("Xóa ảnh")
        .WithDescription("Xóa ảnh khỏi Cloudinary và database")
        .Produces(204)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("AuthenticatedOnly");

        // Xóa tất cả ảnh của đối tượng (Admin/Management)
        imageGroup.MapDelete("/{tableType}/{typeId:int}/all", async (
            string tableType,
            int typeId,
            IImageService imageService) =>
        {
            if (!IsValidTableType(tableType))
                return Results.BadRequest(new { message = "TableType không hợp lệ" });

            var result = await imageService.DeleteAllImagesAsync(tableType, typeId);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteAllImages")
        .WithSummary("Xóa tất cả ảnh")
        .WithDescription("Xóa tất cả ảnh của đối tượng (chỉ Admin/Management)")
        .Produces(204)
        .Produces(400)
        .Produces(404)
        .RequireAuthorization("ManagementOnly");

        // ===== ENDPOINTS ĐẶC BIỆT CHO USER AVATAR =====

        // Lấy avatar của user
        imageGroup.MapGet("/avatar/{userId:int}", async (int userId, IImageService imageService) =>
        {
            var avatar = await imageService.GetUserAvatarAsync(userId);
            return avatar != null 
                ? Results.Ok(avatar) 
                : Results.NotFound();
        })
        .WithName("GetUserAvatar")
        .WithSummary("Lấy avatar user")
        .WithDescription("Lấy ảnh avatar của user (ưu tiên Image table, fallback User.AvatarURL)")
        .Produces(200)
        .Produces(404);

        // Set ảnh làm avatar chính
        imageGroup.MapPost("/avatar/{userId:int}/{imageId:int}", async (
            int userId,
            int imageId,
            IImageService imageService,
            HttpContext context) =>
        {
            // Kiểm tra quyền: chỉ chính user hoặc admin
            var currentUserIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            var currentUserRoleClaim = context.User.FindFirst(ClaimTypes.Role);

            if (currentUserIdClaim == null)
                return Results.Unauthorized();

            var currentUserId = int.Parse(currentUserIdClaim.Value);
            var isAdmin = currentUserRoleClaim?.Value == RoleConstants.Admin;

            if (!isAdmin && currentUserId != userId)
                return Results.Forbid();

            var result = await imageService.SetUserAvatarAsync(userId, imageId);
            return result != null
                ? Results.Ok(result)
                : Results.BadRequest(new { message = "Không thể set avatar" });
        })
        .WithName("SetUserAvatar")
        .WithSummary("Set avatar user")
        .WithDescription("Đặt ảnh làm avatar chính cho user")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("AuthenticatedOnly");
    }

    // ===== HELPER METHODS =====

    // Kiểm tra TableType hợp lệ
    private static bool IsValidTableType(string tableType)
    {
        return tableType is "Tour" or "Hotel" or "HotelRoom" or "User";
    }

    // Kiểm tra quyền upload ảnh
    private static async Task<bool> CheckUploadPermissionAsync(string tableType, int typeId, HttpContext context)
    {
        var currentUserIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        var currentUserRoleClaim = context.User.FindFirst(ClaimTypes.Role);

        if (currentUserIdClaim == null) return false;

        var currentUserId = int.Parse(currentUserIdClaim.Value);
        var currentUserRole = currentUserRoleClaim?.Value;

        // Admin có thể upload cho bất kỳ đối tượng nào
        if (currentUserRole == RoleConstants.Admin)
            return true;

        // Kiểm tra quyền theo từng loại đối tượng
        return tableType switch
        {
            "User" => currentUserId == typeId, // Chỉ upload cho chính mình
            "Tour" => await CheckTourOwnershipAsync(typeId, currentUserId, context),
            "Hotel" => await CheckHotelOwnershipAsync(typeId, currentUserId, context),
            "HotelRoom" => await CheckHotelRoomOwnershipAsync(typeId, currentUserId, context),
            _ => false
        };
    }

    // Kiểm tra quyền thao tác với ảnh
    private static async Task<bool> CheckImagePermissionAsync(int imageId, HttpContext context, IImageService imageService)
    {
        var currentUserIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        var currentUserRoleClaim = context.User.FindFirst(ClaimTypes.Role);

        if (currentUserIdClaim == null) return false;

        var currentUserRole = currentUserRoleClaim?.Value;

        // Admin có thể thao tác với bất kỳ ảnh nào
        if (currentUserRole == RoleConstants.Admin)
            return true;

        // Management roles có thể thao tác với ảnh
        if (RoleConstants.ManagementRoles.Contains(currentUserRole))
            return true;

        // Lấy thông tin ảnh để kiểm tra ownership
        var image = await imageService.GetImageByIdAsync(imageId);
        if (image == null) return false;

        var currentUserId = int.Parse(currentUserIdClaim.Value);

        // Kiểm tra quyền sở hữu theo từng loại đối tượng
        return image.TableType switch
        {
            "User" => currentUserId == image.TypeID, // Chỉ thao tác ảnh của chính mình
            "Tour" => await CheckTourOwnershipAsync(image.TypeID, currentUserId, context),
            "Hotel" => await CheckHotelOwnershipAsync(image.TypeID, currentUserId, context),
            "HotelRoom" => await CheckHotelRoomOwnershipAsync(image.TypeID, currentUserId, context),
            _ => false
        };
    }

    // Kiểm tra quyền sở hữu Tour
    private static Task<bool> CheckTourOwnershipAsync(int tourId, int userId, HttpContext context)
    {
        // TODO: Implement logic kiểm tra user có sở hữu tour không
        // Cần inject ITourRepository hoặc truy vấn DB
        return Task.FromResult(true); // Placeholder
    }

    // Kiểm tra quyền sở hữu Hotel
    private static Task<bool> CheckHotelOwnershipAsync(int hotelId, int userId, HttpContext context)
    {
        // TODO: Implement logic kiểm tra user có sở hữu hotel không
        return Task.FromResult(true); // Placeholder
    }

    // Kiểm tra quyền sở hữu HotelRoom
    private static Task<bool> CheckHotelRoomOwnershipAsync(int roomId, int userId, HttpContext context)
    {
        // TODO: Implement logic kiểm tra user có sở hữu hotel room không
        return Task.FromResult(true); // Placeholder
    }
}
