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
            [FromForm] string tableTypeStr,
            [FromForm] Guid typeId,
            [FromForm] IFormFile file,
            IImageService imageService,
            HttpContext context) =>
        {
            // Validate input
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { message = "File không được để trống" });

            if (string.IsNullOrEmpty(tableTypeStr))
                return Results.BadRequest(new { message = "TableType không được để trống" });

            if (!Enum.TryParse<TableType>(tableTypeStr, true, out var tableType))
                return Results.BadRequest(new { message = "TableType không hợp lệ. Chỉ chấp nhận: Tour, Hotel, User" });

            if (typeId == Guid.Empty)
                return Results.BadRequest(new { message = "TypeID không được để trống" });

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
                    File = file
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
        imageGroup.MapGet("/{tableType}/{typeId:guid}", async (
            string tableTypeStr, 
            Guid typeId, 
            IImageService imageService) =>
        {
            if (!Enum.TryParse<TableType>(tableTypeStr, true, out var tableType))
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

        // Update endpoint removed - Description property no longer exists

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
        imageGroup.MapDelete("/{tableType}/{typeId:guid}/all", async (
            string tableTypeStr,
            Guid typeId,
            IImageService imageService) =>
        {
            if (!Enum.TryParse<TableType>(tableTypeStr, true, out var tableType))
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
        imageGroup.MapGet("/avatar/{userId:guid}", async (Guid userId, IImageService imageService) =>
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
        imageGroup.MapPost("/avatar/{userId:guid}/{imageId:int}", async (
            Guid userId,
            int imageId,
            IImageService imageService,
            HttpContext context) =>
        {
            // Kiểm tra quyền: chỉ chính user hoặc admin
            var currentUserIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            var currentUserRoleClaim = context.User.FindFirst(ClaimTypes.Role);

            if (currentUserIdClaim == null)
                return Results.Unauthorized();

            var currentUserId = Guid.Parse(currentUserIdClaim.Value);
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
    private static async Task<bool> CheckUploadPermissionAsync(TableType tableType, Guid typeId, HttpContext context)
    {
        var currentUserIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        var currentUserRoleClaim = context.User.FindFirst(ClaimTypes.Role);

        if (currentUserIdClaim == null) return false;

        var currentUserId = Guid.Parse(currentUserIdClaim.Value);
        var currentUserRole = currentUserRoleClaim?.Value;

        // Admin có thể upload cho bất kỳ đối tượng nào
        if (currentUserRole == RoleConstants.Admin)
            return true;

        // Kiểm tra quyền theo từng loại đối tượng
        return tableType switch
        {
            TableType.User => currentUserId == typeId, // Chỉ upload cho chính mình
            TableType.Tour => await CheckTourOwnershipAsync(typeId, currentUserId, context),
            TableType.Hotel => await CheckHotelOwnershipAsync(typeId, currentUserId, context),
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

        var currentUserId = Guid.Parse(currentUserIdClaim.Value);

        // Kiểm tra quyền sở hữu theo từng loại đối tượng
        return image.TableType switch
        {
            TableType.User => currentUserId == image.TypeID, // Chỉ thao tác ảnh của chính mình
            TableType.Tour => await CheckTourOwnershipAsync(image.TypeID, currentUserId, context),
            TableType.Hotel => await CheckHotelOwnershipAsync(image.TypeID, currentUserId, context),
            _ => false
        };
    }

    // Kiểm tra quyền sở hữu Tour
    private static Task<bool> CheckTourOwnershipAsync(Guid tourId, Guid userId, HttpContext context)
    {
        // TODO: Implement logic kiểm tra user có sở hữu tour không
        // Cần inject ITourRepository hoặc truy vấn DB
        return Task.FromResult(true); // Placeholder
    }

    // Kiểm tra quyền sở hữu Hotel
    private static Task<bool> CheckHotelOwnershipAsync(Guid hotelId, Guid userId, HttpContext context)
    {
        // TODO: Implement logic kiểm tra user có sở hữu hotel không
        return Task.FromResult(true); // Placeholder
    }

    // Kiểm tra quyền sở hữu HotelRoom
    private static Task<bool> CheckHotelRoomOwnershipAsync(Guid roomId, Guid userId, HttpContext context)
    {
        // TODO: Implement logic kiểm tra user có sở hữu hotel room không
        return Task.FromResult(true); // Placeholder
    }
}
