using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints;

public static class HotelRoomEndpoints
{
    public static void MapHotelRoomEndpoints(this WebApplication app)
    {
        var roomGroup = app.MapGroup("/rooms")
            .WithTags("Hotel Room")
            .WithOpenApi();

        // 1. Thêm phòng mới cho khách sạn (hỗ trợ upload ảnh)
        roomGroup.MapPost("/", async (HttpContext context, [FromServices] IHotelService hotelService, [FromServices] ICloudinaryService cloudinaryService) =>
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
                if (!form.TryGetValue("hotelId", out var hotelIdValue) || !Guid.TryParse(hotelIdValue, out var hotelId))
                    return Results.BadRequest(new { message = "HotelId không hợp lệ" });

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
                    HotelId = hotelId,
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

                return Results.Created($"/rooms/{roomId}", response);
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
            .WithDescription("Chủ khách sạn có thể tạo phòng mới và upload ảnh cùng lúc. Sử dụng multipart/form-data với fields: hotelId, roomName, roomType, address, price, maxPeople và files")
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
                                    ["hotelId"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "uuid",
                                        Description = "ID của khách sạn"
                                    },
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
                                Required = new HashSet<string> { "hotelId", "roomName", "roomType", "address", "price", "maxPeople" }
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
        roomGroup.MapGet("/{roomId:guid}", async (Guid roomId, [FromServices] IHotelService hotelService) =>
        {
            try
            {
                var roomDetail = await hotelService.GetRoomDetailAsync(roomId);
                
                return roomDetail != null 
                    ? Results.Ok(roomDetail)
                    : Results.NotFound(new { message = "Không tìm thấy phòng" });
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi để debug
                Console.WriteLine($"Error in GetRoomDetail: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy chi tiết phòng: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetRoomDetail")
        .WithSummary("Xem chi tiết phòng")
        .WithDescription("Lấy thông tin chi tiết của một phòng cụ thể bao gồm trạng thái phòng")
        .Produces<RoomDetailResponseDTO>(200)
        .Produces(404);

        // 3. Danh sách phòng có phân trang
        roomGroup.MapGet("/hotel/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, int page = 1, int limit = 10) =>
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
        .WithDescription("Lấy danh sách phòng của khách sạn với phân trang bao gồm trạng thái phòng")
        .Produces<PaginatedRoomsResponseDTO>(200);

        // 4. Cập nhật thông tin phòng với ảnh
        roomGroup.MapPut("/", async (HttpContext context, [FromServices] IHotelService hotelService, [FromServices] ICloudinaryService cloudinaryService) =>
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

                // Khởi tạo UpdateRoomDTO
                var updateDto = new UpdateRoomDTO();

                // Xử lý multipart form data
                if (context.Request.HasFormContentType)
                {
                    try
                    {
                        var form = await context.Request.ReadFormAsync();
                        
                        // Validate và parse roomId
                        if (!form.TryGetValue("roomId", out var roomIdValue) || !Guid.TryParse(roomIdValue, out var roomId))
                            return Results.BadRequest(new { message = "RoomId không hợp lệ" });
                        
                        // Lấy thông tin text từ form
                        if (form.ContainsKey("roomName") && !string.IsNullOrWhiteSpace(form["roomName"].FirstOrDefault()))
                            updateDto.RoomName = form["roomName"].FirstOrDefault();
                        
                        if (form.ContainsKey("roomType") && !string.IsNullOrWhiteSpace(form["roomType"].FirstOrDefault()))
                            updateDto.RoomType = form["roomType"].FirstOrDefault();
                        
                        if (form.ContainsKey("address") && !string.IsNullOrWhiteSpace(form["address"].FirstOrDefault()))
                            updateDto.Address = form["address"].FirstOrDefault();

                        if (form.ContainsKey("price") && decimal.TryParse(form["price"].FirstOrDefault(), out var price))
                        {
                            if (price > 0)
                                updateDto.Price = price;
                            else
                                return Results.BadRequest(new { message = "Giá phòng phải lớn hơn 0" });
                        }

                        if (form.ContainsKey("maxPeople") && int.TryParse(form["maxPeople"].FirstOrDefault(), out var maxPeople))
                        {
                            if (maxPeople >= 1 && maxPeople <= 20)
                                updateDto.MaxPeople = maxPeople;
                            else
                                return Results.BadRequest(new { message = "Số lượng người phải từ 1 đến 20" });
                        }

                        // Lấy image action
                        var imageAction = form["imageAction"].FirstOrDefault() ?? "keep";
                        if (imageAction != "keep" && imageAction != "replace")
                        {
                            return Results.BadRequest(new { message = "ImageAction phải là: keep hoặc replace" });
                        }

                        // Xử lý ảnh cũ trước khi upload ảnh mới
                        var deletedImageUrls = new List<string>();
                        if (imageAction == "replace")
                        {
                            deletedImageUrls = await hotelService.DeleteRoomImagesAsync(roomId, userId, imageAction);
                        }

                        // Cập nhật thông tin phòng
                        var success = await hotelService.UpdateRoomAsync(roomId, userId, updateDto);
                        if (!success)
                        {
                            return Results.NotFound(new { message = "Không tìm thấy phòng hoặc bạn không có quyền cập nhật" });
                        }

                        // Xử lý upload ảnh mới nếu có
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

                        // Tạo message dựa trên action
                        string message;
                        if (imageAction == "replace")
                        {
                            message = $"Cập nhật thông tin phòng thành công. Thay thế {deletedImageUrls.Count} ảnh cũ với {uploadedImageUrls.Count} ảnh mới.";
                        }
                        else // keep
                        {
                            message = uploadedImageUrls.Count > 0 
                                ? $"Cập nhật thông tin phòng thành công. Thêm {uploadedImageUrls.Count} ảnh mới (giữ nguyên ảnh cũ)."
                                : "Cập nhật thông tin phòng thành công (không có ảnh mới).";
                        }

                        var response = new UpdateRoomWithImagesResponseDTO
                        {
                            Message = message,
                            UploadedImageUrls = uploadedImageUrls,
                            FailedUploads = failedUploads,
                            DeletedImageUrls = deletedImageUrls,
                            SuccessImageCount = uploadedImageUrls.Count,
                            FailedImageCount = failedUploads.Count,
                            DeletedImageCount = deletedImageUrls.Count,
                            ImageAction = imageAction
                        };

                        return Results.Ok(response);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Multipart parsing failed: {ex.Message}");
                        return Results.BadRequest(new { message = "Lỗi khi xử lý dữ liệu form" });
                    }
                }
                else
                {
                    // Nếu không phải multipart, thử parse JSON
                    try
                    {
                        context.Request.EnableBuffering();
                        context.Request.Body.Position = 0;
                        
                        using var reader = new StreamReader(context.Request.Body);
                        var jsonString = await reader.ReadToEndAsync();
                        
                        if (!string.IsNullOrEmpty(jsonString))
                        {
                            var updateRoomWithIdDto = System.Text.Json.JsonSerializer.Deserialize<UpdateRoomWithIdDTO>(jsonString);
                            if (updateRoomWithIdDto == null)
                                return Results.BadRequest(new { message = "Dữ liệu JSON không hợp lệ" });
                            
                            updateDto = new UpdateRoomDTO
                            {
                                RoomName = updateRoomWithIdDto.RoomName,
                                RoomType = updateRoomWithIdDto.RoomType,
                                Address = updateRoomWithIdDto.Address,
                                Price = updateRoomWithIdDto.Price,
                                MaxPeople = updateRoomWithIdDto.MaxPeople
                            };
                            
                            var roomId = updateRoomWithIdDto.RoomId;
                            
                            // Cập nhật thông tin phòng
                            var success = await hotelService.UpdateRoomAsync(roomId, userId, updateDto);
                            
                            return success 
                                ? Results.Ok(new { message = "Cập nhật thông tin phòng thành công" })
                                : Results.NotFound(new { message = "Không tìm thấy phòng hoặc bạn không có quyền cập nhật" });
                        }
                        else
                        {
                            return Results.BadRequest(new { message = "Dữ liệu JSON không được để trống" });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"JSON parsing failed: {ex.Message}");
                        return Results.BadRequest(new { message = "Lỗi khi xử lý dữ liệu JSON" });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating room with images: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật thông tin phòng: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateRoomWithImages")
        .WithSummary("Cập nhật thông tin phòng với ảnh")
        .WithDescription("Cập nhật thông tin phòng và upload ảnh cùng lúc. Sử dụng multipart/form-data với fields: roomId, roomName, roomType, address, price, maxPeople, imageAction và files. Hoặc application/json với roomId và các trường cần cập nhật.")
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
                                ["roomId"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "uuid",
                                    Description = "ID của phòng cần cập nhật"
                                },
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
                                    Format = "decimal",
                                    Description = "Giá phòng/đêm"
                                },
                                ["maxPeople"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "integer",
                                    Format = "int32",
                                    Description = "Số người tối đa (1-20)"
                                },
                                ["imageAction"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Hành động với ảnh cũ (BẮT BUỘC khi upload files):\n" +
                                                 "• keep: Giữ ảnh cũ + thêm ảnh mới\n" +
                                                 "• replace: Xóa ảnh cũ + thay thế bằng ảnh mới",
                                    Default = new Microsoft.OpenApi.Any.OpenApiString("keep"),
                                    Example = new Microsoft.OpenApi.Any.OpenApiString("replace"),
                                    Enum = new List<Microsoft.OpenApi.Any.IOpenApiAny>
                                    {
                                        new Microsoft.OpenApi.Any.OpenApiString("keep"),
                                        new Microsoft.OpenApi.Any.OpenApiString("replace")
                                    }
                                },
                                ["files"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "array",
                                    Items = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    },
                                    Description = "Danh sách ảnh phòng mới (JPEG, PNG, GIF, WebP, max 5MB/file)"
                                }
                            }
                        }
                    },
                    ["application/json"] = new Microsoft.OpenApi.Models.OpenApiMediaType
                    {
                        Schema = new Microsoft.OpenApi.Models.OpenApiSchema
                        {
                            Type = "object",
                            Properties = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>
                            {
                                ["roomId"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "uuid",
                                    Description = "ID của phòng cần cập nhật"
                                },
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
                                    Format = "decimal",
                                    Description = "Giá phòng/đêm"
                                },
                                ["maxPeople"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "integer",
                                    Format = "int32",
                                    Description = "Số người tối đa (1-20)"
                                }
                            },
                            Required = new HashSet<string> { "roomId" }
                        }
                    }
                }
            }
        })
        .Produces<UpdateRoomWithImagesResponseDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("HotelOnly");


        // 5. Cập nhật trạng thái phòng
        roomGroup.MapPut("/{roomId:guid}/status", async (Guid roomId, [FromBody] UpdateRoomStatusDTO updateDto, [FromServices] IHotelService hotelService, HttpContext context) =>
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
            catch (ArgumentException ex)
            {
                // Xử lý validation errors (status không hợp lệ)
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi để debug
                Console.WriteLine($"Error in UpdateRoomStatus: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật trạng thái phòng: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateRoomStatus")
        .WithSummary("Cập nhật trạng thái phòng")
        .WithDescription("Chủ khách sạn có thể cập nhật trạng thái phòng. Chỉ chấp nhận các giá trị enum hợp lệ: Available, Occupied, Maintenance, Removed")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500)
        .RequireAuthorization("HotelOnly");

        // 6. Xem danh sách phòng theo trạng thái
        roomGroup.MapGet("/hotel/{hotelId:guid}/status", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context, string? status = null) =>
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

        // 7. Lấy danh sách phòng của hotel (không bao gồm status Removed) - Public
        roomGroup.MapGet("/hotel/{hotelId}/active", async (
            Guid hotelId,
            [FromServices] IHotelService hotelService,
            int page = 1,
            int limit = 10) =>
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var result = await hotelService.GetHotelRoomsExcludeRemovedAsync(hotelId, page, limit);
                return Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy danh sách phòng: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelRoomsExcludeRemoved")
        .WithSummary("Lấy danh sách phòng của khách sạn (không bao gồm phòng đã xóa)")
        .WithDescription("Lấy danh sách phòng của một khách sạn, loại trừ những phòng có status Removed")
        .Produces<PaginatedRoomsResponseDTO>(200)
        .Produces(400)
        .Produces(500)
        .AllowAnonymous();
    }

    private static bool IsImageContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;
            
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
