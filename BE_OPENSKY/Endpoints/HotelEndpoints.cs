using BE_OPENSKY.DTOs;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BE_OPENSKY.Endpoints;

public static class HotelEndpoints
{
    public static void MapHotelEndpoints(this WebApplication app)
    {
        var hotelGroup = app.MapGroup("/api/hotels")
            .WithTags("Hotel Management")
            .WithOpenApi();

        // 1. Cập nhật thông tin khách sạn
        hotelGroup.MapPut("/{hotelId:guid}", async (Guid hotelId, [FromBody] UpdateHotelDTO updateDto, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Hotel
                if (!context.User.IsInRole(RoleConstants.Hotel))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Cập nhật thông tin khách sạn
                var success = await hotelService.UpdateHotelAsync(hotelId, userId, updateDto);
                
                return success 
                    ? Results.Ok(new { message = "Cập nhật thông tin khách sạn thành công" })
                    : Results.NotFound(new { message = "Không tìm thấy khách sạn hoặc bạn không có quyền cập nhật" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi cập nhật thông tin khách sạn",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateHotel")
        .WithSummary("Cập nhật thông tin khách sạn")
        .WithDescription("Chủ khách sạn có thể cập nhật thông tin khách sạn của mình")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("HotelOnly");

        // 2. Thêm nhiều ảnh cho khách sạn - Smart endpoint (giống profile avatar)
        hotelGroup.MapPost("/{hotelId:guid}/images", async (Guid hotelId, HttpContext context, [FromServices] IHotelService hotelService, [FromServices] ICloudinaryService cloudinaryService) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Hotel
                if (!context.User.IsInRole(RoleConstants.Hotel))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Kiểm tra quyền sở hữu khách sạn
                var isOwner = await hotelService.IsHotelOwnerAsync(hotelId, userId);
                if (!isOwner)
                {
                    return Results.Json(new { message = "Bạn không có quyền thêm ảnh cho khách sạn này" }, statusCode: 403);
                }

                var contentType = context.Request.ContentType;
                var filesToUpload = new List<IFormFile>();

                // Kiểm tra xem là multipart hay raw binary
                if (context.Request.HasFormContentType)
                {
                    // Multipart form data
                    try
                    {
                        var form = await context.Request.ReadFormAsync();
                        var allFiles = form.Files;

                        // Hỗ trợ nhiều cách đặt tên key
                        if (allFiles.Count == 0)
                        {
                            // Thử các key khác nhau
                            var singleFile = form.Files.GetFile("file") ?? 
                                           form.Files.GetFile("image") ?? 
                                           form.Files.GetFile("files");
                            if (singleFile != null)
                            {
                                filesToUpload.Add(singleFile);
                            }
                        }
                        else
                        {
                            filesToUpload.AddRange(allFiles);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Multipart parsing failed: {ex.Message}");
                        return Results.BadRequest(new { message = "Lỗi khi xử lý multipart form data" });
                    }
                }

                // Nếu không có file từ multipart, thử raw binary
                if (filesToUpload.Count == 0 && IsImageContentType(contentType))
                {
                    // Raw binary upload
                    using var memoryStream = new MemoryStream();
                    await context.Request.Body.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    if (fileBytes.Length == 0)
                    {
                        return Results.BadRequest(new { 
                            message = "Không tìm thấy file. Hãy gửi file dưới dạng multipart/form-data hoặc raw binary với Content-Type image/*",
                            contentType = contentType,
                            suggestion = "Sử dụng form-data với key 'file' hoặc 'files'"
                        });
                    }

                    if (fileBytes.Length > 5 * 1024 * 1024) // 5MB
                    {
                        return Results.BadRequest(new { message = "File không được vượt quá 5MB" });
                    }

                    var fileName = $"hotel_{hotelId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
                    var file = new FormFileFromBytes(fileBytes, fileName, contentType ?? "image/jpeg");
                    filesToUpload.Add(file);
                }

                if (filesToUpload.Count == 0)
                {
                    return Results.BadRequest(new { 
                        message = "Không tìm thấy file. Hãy gửi file dưới dạng multipart/form-data hoặc raw binary với Content-Type image/*",
                        contentType = contentType,
                        supportedFormats = new[] { "multipart/form-data", "image/jpeg", "image/png", "image/gif", "image/webp" }
                    });
                }

                // Validate và upload từng file
                var uploadedUrls = new List<string>();
                var failedFiles = new List<string>();

                foreach (var file in filesToUpload)
                {
                    try
                    {
                        if (!IsImageContentType(file.ContentType))
                        {
                            failedFiles.Add($"{file.FileName} (not an image)");
                            continue;
                        }

                        if (file.Length > 5 * 1024 * 1024) // 5MB per file
                        {
                            failedFiles.Add($"{file.FileName} (too large)");
                            continue;
                        }

                        // Upload lên Cloudinary
                        var imageUrl = await cloudinaryService.UploadImageAsync(file, "hotels");
                        
                        // Lưu vào database
                        var image = new Image
                        {
                            TableType = TableTypeImage.Hotel,
                            TypeID = hotelId,
                            URL = imageUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        using var scope = context.RequestServices.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        dbContext.Images.Add(image);
                        await dbContext.SaveChangesAsync();

                        uploadedUrls.Add(imageUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to upload {file.FileName}: {ex.Message}");
                        failedFiles.Add($"{file.FileName} (upload failed)");
                    }
                }

                return Results.Ok(new MultipleImageUploadResponseDTO
                {
                    UploadedUrls = uploadedUrls,
                    FailedUploads = failedFiles,
                    SuccessCount = uploadedUrls.Count,
                    FailedCount = failedFiles.Count,
                    Message = $"Upload thành công {uploadedUrls.Count}/{filesToUpload.Count} ảnh cho khách sạn"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi upload ảnh: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UploadHotelImages")
        .WithSummary("Thêm ảnh cho khách sạn")
        .WithDescription("Upload ảnh cho khách sạn - hỗ trợ cả multipart/form-data và raw binary")
        .Accepts<IFormFile>("multipart/form-data")
        .Accepts<byte[]>("image/jpeg", "image/png", "image/gif")
        .Produces<MultipleImageUploadResponseDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("HotelOnly");

        // 3. Thêm phòng mới cho khách sạn
        hotelGroup.MapPost("/{hotelId:guid}/rooms", async (Guid hotelId, [FromBody] CreateRoomDTO createRoomDto, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Hotel
                if (!context.User.IsInRole(RoleConstants.Hotel))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(createRoomDto.RoomName))
                    return Results.BadRequest(new { message = "Tên phòng không được để trống" });

                if (string.IsNullOrWhiteSpace(createRoomDto.RoomType))
                    return Results.BadRequest(new { message = "Loại phòng không được để trống" });

                if (createRoomDto.Price <= 0)
                    return Results.BadRequest(new { message = "Giá phòng phải lớn hơn 0" });

                if (createRoomDto.MaxPeople < 1 || createRoomDto.MaxPeople > 20)
                    return Results.BadRequest(new { message = "Số lượng người phải từ 1 đến 20" });

                // Tạo phòng mới
                var roomId = await hotelService.CreateRoomAsync(hotelId, userId, createRoomDto);
                
                return Results.Created($"/api/hotels/{hotelId}/rooms/{roomId}", new { 
                    message = "Tạo phòng thành công",
                    roomId = roomId
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { message = ex.Message }, statusCode: 403);
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                Console.WriteLine($"Error creating room: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi tạo phòng mới: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("CreateRoom")
        .WithSummary("Thêm phòng mới cho khách sạn")
        .WithDescription("Chủ khách sạn có thể tạo phòng mới")
        .Produces(201)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("HotelOnly");

        // 4. Thêm nhiều ảnh cho phòng - Smart endpoint (giống profile avatar)
        hotelGroup.MapPost("/rooms/{roomId:guid}/images", async (Guid roomId, HttpContext context, [FromServices] IHotelService hotelService, [FromServices] ICloudinaryService cloudinaryService) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Hotel
                if (!context.User.IsInRole(RoleConstants.Hotel))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Kiểm tra quyền sở hữu phòng
                var isOwner = await hotelService.IsRoomOwnerAsync(roomId, userId);
                if (!isOwner)
                {
                    return Results.Json(new { message = "Bạn không có quyền thêm ảnh cho phòng này" }, statusCode: 403);
                }

                var contentType = context.Request.ContentType;
                var filesToUpload = new List<IFormFile>();

                // Kiểm tra xem là multipart hay raw binary
                if (context.Request.HasFormContentType)
                {
                    // Multipart form data
                    try
                    {
                        var form = await context.Request.ReadFormAsync();
                        var allFiles = form.Files;

                        // Hỗ trợ nhiều cách đặt tên key
                        if (allFiles.Count == 0)
                        {
                            // Thử các key khác nhau
                            var singleFile = form.Files.GetFile("file") ?? 
                                           form.Files.GetFile("image") ?? 
                                           form.Files.GetFile("files");
                            if (singleFile != null)
                            {
                                filesToUpload.Add(singleFile);
                            }
                        }
                        else
                        {
                            filesToUpload.AddRange(allFiles);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Multipart parsing failed: {ex.Message}");
                        return Results.BadRequest(new { message = "Lỗi khi xử lý multipart form data" });
                    }
                }

                // Nếu không có file từ multipart, thử raw binary
                if (filesToUpload.Count == 0 && IsImageContentType(contentType))
                {
                    // Raw binary upload
                    using var memoryStream = new MemoryStream();
                    await context.Request.Body.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    if (fileBytes.Length == 0)
                    {
                        return Results.BadRequest(new { 
                            message = "Không tìm thấy file. Hãy gửi file dưới dạng multipart/form-data hoặc raw binary với Content-Type image/*",
                            contentType = contentType,
                            suggestion = "Sử dụng form-data với key 'file' hoặc 'files'"
                        });
                    }

                    if (fileBytes.Length > 5 * 1024 * 1024) // 5MB
                    {
                        return Results.BadRequest(new { message = "File không được vượt quá 5MB" });
                    }

                    var fileName = $"room_{roomId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
                    var file = new FormFileFromBytes(fileBytes, fileName, contentType ?? "image/jpeg");
                    filesToUpload.Add(file);
                }

                if (filesToUpload.Count == 0)
                {
                    return Results.BadRequest(new { 
                        message = "Không tìm thấy file. Hãy gửi file dưới dạng multipart/form-data hoặc raw binary với Content-Type image/*",
                        contentType = contentType,
                        supportedFormats = new[] { "multipart/form-data", "image/jpeg", "image/png", "image/gif", "image/webp" }
                    });
                }

                // Validate và upload từng file
                var uploadedUrls = new List<string>();
                var failedFiles = new List<string>();

                foreach (var file in filesToUpload)
                {
                    try
                    {
                        if (!IsImageContentType(file.ContentType))
                        {
                            failedFiles.Add($"{file.FileName} (not an image)");
                            continue;
                        }

                        if (file.Length > 5 * 1024 * 1024) // 5MB per file
                        {
                            failedFiles.Add($"{file.FileName} (too large)");
                            continue;
                        }

                        // Upload lên Cloudinary
                        var imageUrl = await cloudinaryService.UploadImageAsync(file, "rooms");
                        
                        // Lưu vào database
                        var image = new Image
                        {
                            TableType = TableTypeImage.RoomHotel,
                            TypeID = roomId,
                            URL = imageUrl,
                            CreatedAt = DateTime.UtcNow
                        };

                        using var scope = context.RequestServices.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        dbContext.Images.Add(image);
                        await dbContext.SaveChangesAsync();

                        uploadedUrls.Add(imageUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to upload {file.FileName}: {ex.Message}");
                        failedFiles.Add($"{file.FileName} (upload failed)");
                    }
                }

                return Results.Ok(new MultipleImageUploadResponseDTO
                {
                    UploadedUrls = uploadedUrls,
                    FailedUploads = failedFiles,
                    SuccessCount = uploadedUrls.Count,
                    FailedCount = failedFiles.Count,
                    Message = $"Upload thành công {uploadedUrls.Count}/{filesToUpload.Count} ảnh cho phòng"
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi upload ảnh: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UploadRoomImages")
        .WithSummary("Thêm ảnh cho phòng")
        .WithDescription("Upload ảnh cho phòng - hỗ trợ cả multipart/form-data và raw binary")
        .Accepts<IFormFile>("multipart/form-data")
        .Accepts<byte[]>("image/jpeg", "image/png", "image/gif")
        .Produces<MultipleImageUploadResponseDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("HotelOnly");

        // 5. Xem chi tiết khách sạn (với phân trang phòng)
        hotelGroup.MapGet("/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, int page = 1, int limit = 10) =>
        {
            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var hotelDetail = await hotelService.GetHotelDetailAsync(hotelId, page, limit);
                
                return hotelDetail != null 
                    ? Results.Ok(hotelDetail)
                    : Results.NotFound(new { message = "Không tìm thấy khách sạn hoặc khách sạn chưa được kích hoạt" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi lấy chi tiết khách sạn",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelDetail")
        .WithSummary("Xem chi tiết khách sạn - có danh sách phòng")
        .WithDescription("Lấy thông tin chi tiết khách sạn bao gồm ảnh và danh sách phòng có phân trang")
        .Produces<HotelDetailResponseDTO>(200)
        .Produces(404);

        // 6. Xem chi tiết phòng
        hotelGroup.MapGet("/rooms/{roomId:guid}", async (Guid roomId, [FromServices] IHotelService hotelService) =>
        {
            try
            {
                var roomDetail = await hotelService.GetRoomDetailAsync(roomId);
                
                return roomDetail != null 
                    ? Results.Ok(roomDetail)
                    : Results.NotFound(new { message = "Không tìm thấy phòng" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi lấy chi tiết phòng",
                    statusCode: 500
                );
            }
        })
        .WithName("GetRoomDetail")
        .WithSummary("Xem chi tiết phòng")
        .WithDescription("Lấy thông tin chi tiết của một phòng cụ thể")
        .Produces<RoomDetailResponseDTO>(200)
        .Produces(404);

        // 7. Danh sách phòng có phân trang (endpoint riêng)
        hotelGroup.MapGet("/{hotelId:guid}/rooms", async (Guid hotelId, [FromServices] IHotelService hotelService, int page = 1, int limit = 10) =>
        {
            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var paginatedRooms = await hotelService.GetHotelRoomsAsync(hotelId, page, limit);
                
                return Results.Ok(paginatedRooms);
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi lấy danh sách phòng",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelRooms")
        .WithSummary("Danh sách phòng có phân trang")
        .WithDescription("Lấy danh sách phòng của khách sạn với phân trang")
        .Produces<PaginatedRoomsResponseDTO>(200);

        // Bonus: Cập nhật thông tin phòng
        hotelGroup.MapPut("/rooms/{roomId:guid}", async (Guid roomId, [FromBody] UpdateRoomDTO updateDto, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Hotel
                if (!context.User.IsInRole(RoleConstants.Hotel))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Cập nhật thông tin phòng
                var success = await hotelService.UpdateRoomAsync(roomId, userId, updateDto);
                
                return success 
                    ? Results.Ok(new { message = "Cập nhật thông tin phòng thành công" })
                    : Results.NotFound(new { message = "Không tìm thấy phòng hoặc bạn không có quyền cập nhật" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi cập nhật thông tin phòng",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateRoom")
        .WithSummary("Cập nhật thông tin phòng")
        .WithDescription("Chủ khách sạn có thể cập nhật thông tin phòng")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("HotelOnly");

        // Bonus: Xóa phòng
        hotelGroup.MapDelete("/rooms/{roomId:guid}", async (Guid roomId, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Hotel
                if (!context.User.IsInRole(RoleConstants.Hotel))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Xóa phòng
                var success = await hotelService.DeleteRoomAsync(roomId, userId);
                
                return success 
                    ? Results.Ok(new { message = "Xóa phòng thành công" })
                    : Results.NotFound(new { message = "Không tìm thấy phòng hoặc bạn không có quyền xóa" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi xóa phòng",
                    statusCode: 500
                );
            }
        })
        .WithName("DeleteRoom")
        .WithSummary("Xóa phòng")
        .WithDescription("Chủ khách sạn có thể xóa phòng (bao gồm tất cả ảnh của phòng)")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("HotelOnly");

        // 8. Tìm kiếm và lọc khách sạn (Public - không cần auth)
        hotelGroup.MapGet("/search", async (
            IHotelService hotelService,
            string? q = null,
            string? province = null,
            string? address = null,
            string? stars = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sortBy = "name",
            string? sortOrder = "asc",
            int page = 1,
            int limit = 10) =>
        {
            try
            {
                // Parse stars parameter
                var starsList = new List<int>();
                if (!string.IsNullOrEmpty(stars))
                {
                    var starValues = stars.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var starValue in starValues)
                    {
                        if (int.TryParse(starValue.Trim(), out var star) && star >= 1 && star <= 5)
                        {
                            starsList.Add(star);
                        }
                    }
                }

                // Validate parameters
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var searchDto = new HotelSearchDTO
                {
                    Query = q,
                    Province = province,
                    Address = address,
                    Stars = starsList.Any() ? starsList : null,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    SortBy = sortBy,
                    SortOrder = sortOrder,
                    Page = page,
                    Limit = limit
                };

                var result = await hotelService.SearchHotelsAsync(searchDto);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi tìm kiếm khách sạn: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("SearchHotels")
        .WithSummary("Tìm kiếm và lọc khách sạn")
        .WithDescription("Tìm kiếm khách sạn theo tên, địa chỉ, tỉnh, số sao, giá phòng. Hỗ trợ sắp xếp và phân trang.")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Tìm kiếm và lọc khách sạn",
            Description = "Tìm kiếm khách sạn theo tên, địa chỉ, tỉnh, số sao, giá phòng. Hỗ trợ sắp xếp và phân trang.",
            Parameters = new List<OpenApiParameter>
            {
                new() { Name = "q", In = ParameterLocation.Query, Description = "Tìm kiếm theo tên khách sạn hoặc mô tả", Required = false, Schema = new() { Type = "string" } },
                new() { Name = "province", In = ParameterLocation.Query, Description = "Lọc theo tỉnh/thành phố", Required = false, Schema = new() { Type = "string" } },
                new() { Name = "address", In = ParameterLocation.Query, Description = "Lọc theo địa chỉ", Required = false, Schema = new() { Type = "string" } },
                new() { Name = "stars", In = ParameterLocation.Query, Description = "Lọc theo số sao (cách nhau bằng dấu phẩy, ví dụ: 4,5)", Required = false, Schema = new() { Type = "string" } },
                new() { Name = "minPrice", In = ParameterLocation.Query, Description = "Giá phòng tối thiểu (VND)", Required = false, Schema = new() { Type = "number", Format = "decimal" } },
                new() { Name = "maxPrice", In = ParameterLocation.Query, Description = "Giá phòng tối đa (VND)", Required = false, Schema = new() { Type = "number", Format = "decimal" } },
                new() { Name = "sortBy", In = ParameterLocation.Query, Description = "Sắp xếp theo: name, price, star, createdAt", Required = false, Schema = new() { Type = "string" } },
                new() { Name = "sortOrder", In = ParameterLocation.Query, Description = "Thứ tự sắp xếp: asc, desc", Required = false, Schema = new() { Type = "string" } },
                new() { Name = "page", In = ParameterLocation.Query, Description = "Số trang (mặc định: 1)", Required = false, Schema = new() { Type = "integer", Minimum = 1 } },
                new() { Name = "limit", In = ParameterLocation.Query, Description = "Số kết quả mỗi trang (mặc định: 10, tối đa: 100)", Required = false, Schema = new() { Type = "integer", Minimum = 1, Maximum = 100 } }
            }
        })
        .Produces<HotelSearchResponseDTO>(200)
        .Produces(500)
        .AllowAnonymous(); // Public endpoint - không cần authentication

        // 9. Cập nhật trạng thái phòng
        hotelGroup.MapPut("/rooms/{roomId:guid}/status", async (Guid roomId, [FromBody] UpdateRoomStatusDTO updateDto, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Hotel
                if (!context.User.IsInRole(RoleConstants.Hotel))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Cập nhật trạng thái phòng
                var success = await hotelService.UpdateRoomStatusAsync(roomId, userId, updateDto);
                
                return success 
                    ? Results.Ok(new { message = "Cập nhật trạng thái phòng thành công" })
                    : Results.NotFound(new { message = "Không tìm thấy phòng hoặc bạn không có quyền cập nhật" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi cập nhật trạng thái phòng",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateRoomStatus")
        .WithSummary("Cập nhật trạng thái phòng")
        .WithDescription("Chủ khách sạn có thể cập nhật trạng thái phòng (Available, Occupied, Maintenance)")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("HotelOnly");

        // 10. Xem danh sách phòng theo trạng thái
        hotelGroup.MapGet("/{hotelId:guid}/rooms/status", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context, string? status = null) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Hotel
                if (!context.User.IsInRole(RoleConstants.Hotel))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Kiểm tra quyền sở hữu khách sạn
                var isOwner = await hotelService.IsHotelOwnerAsync(hotelId, userId);
                if (!isOwner)
                {
                    return Results.Json(new { message = "Bạn không có quyền xem phòng của khách sạn này" }, statusCode: 403);
                }

                var roomStatusList = await hotelService.GetRoomStatusListAsync(hotelId, status);
                return Results.Ok(roomStatusList);
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi lấy danh sách trạng thái phòng",
                    statusCode: 500
                );
            }
        })
        .WithName("GetRoomStatusList")
        .WithSummary("Xem danh sách phòng theo trạng thái")
        .WithDescription("Chủ khách sạn có thể xem danh sách phòng theo trạng thái (có thể lọc theo status)")
        .Produces<RoomStatusListDTO>(200)
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("HotelOnly");

            // 11. Hotel xem danh sách booking
            hotelGroup.MapGet("/{hotelId:guid}/bookings", async (Guid hotelId, [FromServices] IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var bookings = await bookingService.GetHotelBookingsAsync(hotelId, userIdGuid);
                    return Results.Ok(bookings);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 403);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetHotelBookings")
            .WithSummary("Xem danh sách booking của khách sạn")
            .WithDescription("Hotel xem danh sách booking của khách sạn mình")
            .Produces<BookingListDTO>(200)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization("HotelOnly");
        }

        private static bool IsImageContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;
                
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }
    
}
