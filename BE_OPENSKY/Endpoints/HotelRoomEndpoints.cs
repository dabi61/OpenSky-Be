using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints;

public static class HotelRoomEndpoints
{
    public static void MapHotelRoomEndpoints(this WebApplication app)
    {
        var roomGroup = app.MapGroup("/hotels")
            .WithTags("Hotel Room")
            .WithOpenApi();

        // 1. Thêm phòng mới cho khách sạn (hỗ trợ upload ảnh)
        roomGroup.MapPost("/{hotelId:guid}/rooms", async (Guid hotelId, HttpContext context, [FromServices] IHotelService hotelService, [FromServices] ICloudinaryService cloudinaryService) =>
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

                // Kiểm tra content type
                if (!context.Request.HasFormContentType)
                {
                    return Results.BadRequest(new { message = "Request phải là multipart/form-data để upload ảnh cùng lúc" });
                }

                // Đọc form data
                var form = await context.Request.ReadFormAsync();
                
                // Validate và parse room data
                if (!form.TryGetValue("roomName", out var roomNameValue) || string.IsNullOrWhiteSpace(roomNameValue))
                    return Results.BadRequest(new { message = "Tên phòng không được để trống" });

                if (!form.TryGetValue("roomType", out var roomTypeValue) || string.IsNullOrWhiteSpace(roomTypeValue))
                    return Results.BadRequest(new { message = "Loại phòng không được để trống" });

                if (!form.TryGetValue("address", out var addressValue) || string.IsNullOrWhiteSpace(addressValue))
                    return Results.BadRequest(new { message = "Địa chỉ phòng không được để trống" });

                if (!decimal.TryParse(form["price"], out var price) || price <= 0)
                    return Results.BadRequest(new { message = "Giá phòng phải là số dương" });

                if (!int.TryParse(form["maxPeople"], out var maxPeople) || maxPeople < 1 || maxPeople > 20)
                    return Results.BadRequest(new { message = "Số lượng người phải từ 1 đến 20" });

                // Tạo DTO cho room
                var createRoomDto = new CreateRoomDTO
                {
                    RoomName = roomNameValue.ToString().Trim(),
                    RoomType = roomTypeValue.ToString().Trim(),
                    Address = addressValue.ToString().Trim(),
                    Price = price,
                    MaxPeople = maxPeople
                };

                // Tạo phòng mới
                var roomId = await hotelService.CreateRoomAsync(hotelId, userId, createRoomDto);

                // Xử lý upload ảnh nếu có
                var uploadedImageUrls = new List<string>();
                var failedUploads = new List<string>();

                if (form.Files.Count > 0)
                {
                    foreach (var file in form.Files)
                    {
                        try
                        {
                            if (!IsImageContentType(file.ContentType))
                            {
                                failedUploads.Add($"{file.FileName} (không phải ảnh)");
                                continue;
                            }

                            if (file.Length > 5 * 1024 * 1024) // 5MB per file
                            {
                                failedUploads.Add($"{file.FileName} (quá lớn)");
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

                            uploadedImageUrls.Add(imageUrl);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to upload {file.FileName}: {ex.Message}");
                            failedUploads.Add($"{file.FileName} (lỗi upload: {ex.Message})");
                        }
                    }
                }
                
                var response = new CreateRoomWithImagesResponseDTO
                {
                    RoomID = roomId,
                    Message = uploadedImageUrls.Count > 0 
                        ? $"Tạo phòng thành công với {uploadedImageUrls.Count} ảnh"
                        : "Tạo phòng thành công (không có ảnh)",
                    UploadedImageUrls = uploadedImageUrls,
                    FailedUploads = failedUploads,
                    SuccessImageCount = uploadedImageUrls.Count,
                    FailedImageCount = failedUploads.Count
                };

                return Results.Created($"/hotels/{hotelId}/rooms/{roomId}", response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Results.Json(new { message = ex.Message }, statusCode: 403);
            }
            catch (Exception ex)
            {
                // Log the actual error for debugging
                Console.WriteLine($"Error creating room with images: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi tạo phòng mới: {ex.Message}",
                    statusCode: 500
                );
            }
        })
            .WithName("CreateRoomWithImages")
            .WithSummary("Thêm phòng mới cho khách sạn (có ảnh)")
            .WithDescription("Chủ khách sạn có thể tạo phòng mới và upload ảnh cùng lúc. Sử dụng multipart/form-data với fields: roomName, roomType, address, price, maxPeople và files")
            .WithOpenApi(operation => new Microsoft.OpenApi.Models.OpenApiOperation(operation)
            {
                RequestBody = new Microsoft.OpenApi.Models.OpenApiRequestBody
                {
                    Content = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiMediaType>
                    {
                        ["multipart/form-data"] = new Microsoft.OpenApi.Models.OpenApiMediaType
                        {
                            Schema = new Microsoft.OpenApi.Models.OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>
                                {
                                    ["roomName"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "Tên phòng"
                                    },
                                    ["roomType"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "Loại phòng (Deluxe, Standard, Suite...)"
                                    },
                                    ["address"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "string",
                                        Description = "Địa chỉ phòng"
                                    },
                                    ["price"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "number",
                                        Format = "double",
                                        Description = "Giá phòng/đêm"
                                    },
                                    ["maxPeople"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "integer",
                                        Format = "int32",
                                        Description = "Số người tối đa (1-20)"
                                    },
                                    ["files"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "array",
                                        Items = new Microsoft.OpenApi.Models.OpenApiSchema
                                        {
                                            Type = "string",
                                            Format = "binary"
                                        },
                                        Description = "Danh sách ảnh phòng (JPEG, PNG, GIF, WebP, max 5MB/file)"
                                    }
                                },
                                Required = new HashSet<string> { "roomName", "roomType", "address", "price", "maxPeople" }
                            }
                        }
                    }
                }
            })
            .Produces<CreateRoomWithImagesResponseDTO>(201)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization("HotelOnly");


        // 2. Xem chi tiết phòng
        roomGroup.MapGet("/rooms/{roomId:guid}", async (Guid roomId, [FromServices] IHotelService hotelService) =>
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

        // 3. Danh sách phòng có phân trang
        roomGroup.MapGet("/{hotelId:guid}/rooms", async (Guid hotelId, [FromServices] IHotelService hotelService, int page = 1, int limit = 10) =>
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

        // 4. Cập nhật thông tin phòng
        roomGroup.MapPut("/rooms/{roomId:guid}", async (Guid roomId, [FromBody] UpdateRoomDTO updateDto, [FromServices] IHotelService hotelService, HttpContext context) =>
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

        // 5. Xóa phòng
        roomGroup.MapDelete("/rooms/{roomId:guid}", async (Guid roomId, [FromServices] IHotelService hotelService, HttpContext context) =>
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

        // 6. Cập nhật trạng thái phòng
        roomGroup.MapPut("/rooms/{roomId:guid}/status", async (Guid roomId, [FromBody] UpdateRoomStatusDTO updateDto, [FromServices] IHotelService hotelService, HttpContext context) =>
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

        // 7. Xem danh sách phòng theo trạng thái
        roomGroup.MapGet("/{hotelId:guid}/rooms/status", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context, string? status = null) =>
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
    }

    private static bool IsImageContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;
            
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
