using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints;

public static class HotelEndpoints
{
    public static void MapHotelEndpoints(this WebApplication app)
    {
        var hotelGroup = app.MapGroup("/hotels")
            .WithTags("Hotel")
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

        // 3. Xem chi tiết khách sạn (với phân trang phòng)
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

        // 4. Tìm kiếm và lọc khách sạn (Public - không cần auth)
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

        // 5. Customer đăng ký mở khách sạn (chuyển từ Customer -> Hotel sau khi được duyệt)
        hotelGroup.MapPost("/apply", async ([FromBody] HotelApplicationDTO applicationDto, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra user hiện tại có phải Customer không
                if (!context.User.IsInRole(RoleConstants.Customer))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(applicationDto.HotelName))
                    return Results.BadRequest(new { message = "Tên khách sạn không được để trống" });
                
                if (string.IsNullOrWhiteSpace(applicationDto.Address))
                    return Results.BadRequest(new { message = "Địa chỉ không được để trống" });
                
                if (string.IsNullOrWhiteSpace(applicationDto.Province))
                    return Results.BadRequest(new { message = "Tỉnh/Thành phố không được để trống" });

                if (applicationDto.Latitude < -90 || applicationDto.Latitude > 90)
                    return Results.BadRequest(new { message = "Vĩ độ phải từ -90 đến 90" });

                if (applicationDto.Longitude < -180 || applicationDto.Longitude > 180)
                    return Results.BadRequest(new { message = "Kinh độ phải từ -180 đến 180" });

                if (applicationDto.Star < 1 || applicationDto.Star > 5)
                    return Results.BadRequest(new { message = "Số sao phải từ 1 đến 5" });

                // Tạo đơn đăng ký khách sạn
                var hotelId = await hotelService.CreateHotelApplicationAsync(userId, applicationDto);
                
                return Results.Ok(new { 
                    message = "Đơn đăng ký khách sạn đã được gửi thành công. Vui lòng chờ Admin duyệt.",
                    hotelId = hotelId,
                    status = "Inactive"
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi gửi đơn đăng ký khách sạn",
                    statusCode: 500
                );
            }
        })
        .WithName("ApplyHotel")
        .WithSummary("Customer đăng ký mở khách sạn")
        .WithDescription("Customer có thể đăng ký để trở thành Hotel sau khi được duyệt")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("CustomerOnly");

        // 6. Admin xem tất cả đơn đăng ký khách sạn chờ duyệt
        hotelGroup.MapGet("/pending", async ([FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Admin
                if (!context.User.IsInRole(RoleConstants.Admin))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                var pendingHotels = await hotelService.GetPendingHotelsAsync();
                return Results.Ok(pendingHotels);
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi lấy danh sách đơn đăng ký khách sạn",
                    statusCode: 500
                );
            }
        })
        .WithName("GetPendingHotels")
        .WithSummary("Admin xem tất cả đơn đăng ký khách sạn chờ duyệt")
        .WithDescription("Admin có thể xem danh sách khách sạn có status Inactive (chờ duyệt)")
        .Produces<List<PendingHotelResponseDTO>>(200)
        .Produces(403)
        .RequireAuthorization("AdminOnly");

        // 7. Admin xem chi tiết đơn đăng ký khách sạn
        hotelGroup.MapGet("/pending/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Admin
                if (!context.User.IsInRole(RoleConstants.Admin))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                var hotel = await hotelService.GetHotelByIdAsync(hotelId);
                
                return hotel != null 
                    ? Results.Ok(hotel)
                    : Results.NotFound(new { message = "Không tìm thấy khách sạn" });
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
        .WithName("GetPendingHotelById")
        .WithSummary("Admin xem chi tiết đơn đăng ký khách sạn")
        .WithDescription("Admin có thể xem chi tiết một khách sạn chờ duyệt")
        .Produces<PendingHotelResponseDTO>(200)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("AdminOnly");

        // 8. Admin duyệt đơn đăng ký khách sạn
        hotelGroup.MapPost("/approve/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Admin
                if (!context.User.IsInRole(RoleConstants.Admin))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Lấy Admin ID từ JWT token
                var adminIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var success = await hotelService.ApproveHotelAsync(hotelId, adminId);
                
                return success 
                    ? Results.Ok(new { message = "Đơn đăng ký khách sạn đã được duyệt thành công. Customer đã được chuyển thành Hotel." })
                    : Results.BadRequest(new { message = "Không tìm thấy khách sạn hoặc khách sạn đã được xử lý trước đó" });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi duyệt đơn đăng ký khách sạn",
                    statusCode: 500
                );
            }
        })
        .WithName("ApproveHotel")
        .WithSummary("Admin duyệt đơn đăng ký khách sạn")
        .WithDescription("Admin có thể duyệt khách sạn chờ duyệt (chuyển từ Inactive thành Active và user thành Hotel)")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("AdminOnly");

        // 9. Admin từ chối đơn đăng ký khách sạn
        hotelGroup.MapDelete("/reject/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Admin
                if (!context.User.IsInRole(RoleConstants.Admin))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                var success = await hotelService.RejectHotelAsync(hotelId);
                
                return success 
                    ? Results.Ok(new { message = "Đơn đăng ký khách sạn đã bị từ chối và xóa khỏi hệ thống." })
                    : Results.BadRequest(new { message = "Không tìm thấy khách sạn hoặc khách sạn đã được xử lý trước đó" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi từ chối đơn đăng ký khách sạn",
                    statusCode: 500
                );
            }
        })
        .WithName("RejectHotel")
        .WithSummary("Admin từ chối đơn đăng ký khách sạn")
        .WithDescription("Admin có thể từ chối và xóa khách sạn chờ duyệt")
        .Produces(200)
        .Produces(400)
        .Produces(403)
        .RequireAuthorization("AdminOnly");

        // 10. Customer xem đơn đăng ký khách sạn của mình
        hotelGroup.MapGet("/my-hotels", async ([FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var userHotels = await hotelService.GetUserHotelsAsync(userId);
                return Results.Ok(userHotels);
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi lấy danh sách khách sạn của bạn",
                    statusCode: 500
                );
            }
        })
        .WithName("GetMyHotels")
        .WithSummary("Customer xem khách sạn của mình")
        .WithDescription("Customer có thể xem tất cả khách sạn đã đăng ký (cả chờ duyệt và đã duyệt)")
        .Produces<List<PendingHotelResponseDTO>>(200)
        .Produces(401)
        .RequireAuthorization("AuthenticatedOnly");
        }

        private static bool IsImageContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;
                
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }
}