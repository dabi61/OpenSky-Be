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

                // Kiểm tra quyền tạo phòng sẽ được xác thực sau khi có hotelId

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

                // Cho phép chủ sở hữu khách sạn tạo phòng (không cần role Hotel), miễn hotel không phải Removed
                var isHotelOwner = await hotelService.IsHotelOwnerAsync(hotelId, userId);
                if (!isHotelOwner)
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                if (!form.TryGetValue("roomName", out var roomNameValue) || string.IsNullOrWhiteSpace(roomNameValue))
                    return Results.BadRequest(new { message = "Tên phòng không được để trống" });

                // Validate roomName với regex (hỗ trợ tiếng Việt có dấu)
                var roomNameRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,255}$");
                if (!roomNameRegex.IsMatch(roomNameValue.ToString()))
                    return Results.BadRequest(new { message = "Tên phòng chứa ký tự không hợp lệ" });

                if (!form.TryGetValue("roomType", out var roomTypeValue) || string.IsNullOrWhiteSpace(roomTypeValue))
                    return Results.BadRequest(new { message = "Loại phòng không được để trống" });

                if (!form.TryGetValue("address", out var addressValue) || string.IsNullOrWhiteSpace(addressValue))
                    return Results.BadRequest(new { message = "Địa chỉ phòng không được để trống" });

                // Validate address với regex (hỗ trợ tiếng Việt có dấu)
                var addressRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,255}$");
                if (!addressRegex.IsMatch(addressValue.ToString()))
                    return Results.BadRequest(new { message = "Địa chỉ chứa ký tự không hợp lệ" });

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

                // Kiểm tra ảnh trước khi tạo room
                var imageValidationResult = ValidateRoomImages(form.Files);
                if (!imageValidationResult.IsValid)
                {
                    return Results.BadRequest(new { 
                        message = "Có ảnh không hợp lệ", 
                        invalidFiles = imageValidationResult.InvalidFiles 
                    });
                }

                // Tạo phòng mới
                var roomId = await hotelService.CreateRoomAsync(hotelId, userId, createRoomDto);

                // Xử lý upload ảnh mới (NewImages) - sử dụng logic mới
                var imageResponse = await ProcessNewRoomImagesAsync(roomId, form.Files, cloudinaryService, context);
                
                var response = new CreateRoomWithImagesResponseDTO
                {
                    RoomID = roomId,
                    Message = imageResponse.NewImageCount > 0 
                        ? $"Tạo phòng thành công với {imageResponse.NewImageCount} ảnh"
                        : "Tạo phòng thành công (không có ảnh)",
                    UploadedImageUrls = imageResponse.NewImageUrls,
                    FailedUploads = imageResponse.FailedUploads,
                    SuccessImageCount = imageResponse.NewImageCount,
                    FailedImageCount = imageResponse.FailedImageCount
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
        roomGroup.MapPut("/{roomId:guid}", async (Guid roomId, HttpContext context, [FromServices] IHotelService hotelService, [FromServices] ICloudinaryService cloudinaryService) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Cho phép chủ sở hữu phòng cập nhật status (không cần role Hotel)
                var isOwner = await hotelService.IsRoomOwnerAsync(roomId, userId);
                if (!isOwner)
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
                        
                        // Lấy thông tin text từ form
                        if (form.ContainsKey("roomName") && !string.IsNullOrWhiteSpace(form["roomName"].FirstOrDefault()))
                        {
                            var roomNameValue = form["roomName"].FirstOrDefault();
                            // Validate roomName với regex (hỗ trợ tiếng Việt có dấu)
                            var roomNameRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,255}$");
                            if (!roomNameRegex.IsMatch(roomNameValue))
                                return Results.BadRequest(new { message = "Tên phòng chứa ký tự không hợp lệ" });
                            
                            updateDto.RoomName = roomNameValue;
                        }
                        
                        if (form.ContainsKey("roomType") && !string.IsNullOrWhiteSpace(form["roomType"].FirstOrDefault()))
                            updateDto.RoomType = form["roomType"].FirstOrDefault();
                        
                        if (form.ContainsKey("address") && !string.IsNullOrWhiteSpace(form["address"].FirstOrDefault()))
                        {
                            var addressValue = form["address"].FirstOrDefault();
                            // Validate address với regex (hỗ trợ tiếng Việt có dấu)
                            var addressRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,255}$");
                            if (!addressRegex.IsMatch(addressValue))
                                return Results.BadRequest(new { message = "Địa chỉ chứa ký tự không hợp lệ" });
                            
                            updateDto.Address = addressValue;
                        }

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

                        // Lấy ExistingImageIds (IDs của ảnh muốn giữ lại)
                        var existingImageIds = new List<int>();
                        if (form.ContainsKey("existingImageIds"))
                        {
                            var existingIdsString = form["existingImageIds"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(existingIdsString))
                            {
                                existingImageIds = existingIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Where(id => int.TryParse(id.Trim(), out _))
                                    .Select(int.Parse)
                                    .ToList();
                            }
                        }

                        // Lấy DeleteImageIds (IDs của ảnh muốn xóa)
                        var deleteImageIds = new List<int>();
                        if (form.ContainsKey("deleteImageIds"))
                        {
                            var deleteIdsString = form["deleteImageIds"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(deleteIdsString))
                            {
                                deleteImageIds = deleteIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Where(id => int.TryParse(id.Trim(), out _))
                                    .Select(int.Parse)
                                    .ToList();
                            }
                        }

                        // Kiểm tra ảnh trước khi cập nhật room
                        var imageValidationResult = ValidateRoomImages(form.Files);
                        if (!imageValidationResult.IsValid)
                        {
                            return Results.BadRequest(new { 
                                message = "Có ảnh không hợp lệ", 
                                invalidFiles = imageValidationResult.InvalidFiles 
                            });
                        }

                        // Cập nhật thông tin phòng
                        var success = await hotelService.UpdateRoomAsync(roomId, userId, updateDto);
                        if (!success)
                        {
                            return Results.NotFound(new { message = "Không tìm thấy phòng hoặc bạn không có quyền cập nhật" });
                        }

                        // Xử lý ảnh theo logic mới
                        var imageUpdateDto = new RoomImageUpdateDTO
                        {
                            RoomId = roomId,
                            RoomName = updateDto.RoomName,
                            RoomType = updateDto.RoomType,
                            Address = updateDto.Address,
                            Price = updateDto.Price,
                            MaxPeople = updateDto.MaxPeople,
                            ExistingImageIds = existingImageIds.Any() ? existingImageIds : null,
                            DeleteImageIds = deleteImageIds.Any() ? deleteImageIds : null
                        };

                        var imageResponse = await ProcessRoomImagesAsync(roomId, imageUpdateDto, form.Files, cloudinaryService, context);

                        return Results.Ok(imageResponse);
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
                            var updateRoomDto = System.Text.Json.JsonSerializer.Deserialize<UpdateRoomDTO>(jsonString);
                            if (updateRoomDto == null)
                                return Results.BadRequest(new { message = "Dữ liệu JSON không hợp lệ" });
                            
                            updateDto = updateRoomDto;
                            
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
        .WithDescription("Cập nhật thông tin phòng và upload ảnh cùng lúc. Sử dụng multipart/form-data với fields: roomName, roomType, address, price, maxPeople, existingImageIds, deleteImageIds và files. Hoặc application/json với các trường cần cập nhật.")
        .WithOpenApi(operation => new Microsoft.OpenApi.Models.OpenApiOperation(operation)
        {
            Parameters = new List<Microsoft.OpenApi.Models.OpenApiParameter>
            {
                new Microsoft.OpenApi.Models.OpenApiParameter
                {
                    Name = "roomId",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Path,
                    Required = true,
                    Schema = new Microsoft.OpenApi.Models.OpenApiSchema
                    {
                        Type = "string",
                        Format = "uuid"
                    }
                }
            },
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
                                    Format = "decimal",
                                    Description = "Giá phòng/đêm"
                                },
                                ["maxPeople"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "integer",
                                    Format = "int32",
                                    Description = "Số người tối đa (1-20)"
                                },
                                ["existingImageIds"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "IDs của ảnh muốn giữ lại (cách nhau bởi dấu phẩy), ví dụ: '1,2,3'"
                                },
                                ["deleteImageIds"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "IDs của ảnh muốn xóa (cách nhau bởi dấu phẩy), ví dụ: '4,5'"
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
                            }
                        }
                    }
                }
            }
        })
        .Produces<RoomImageUpdateResponseDTO>(200)
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

    private static ImageValidationResult ValidateRoomImages(IFormFileCollection files)
    {
        var result = new ImageValidationResult { IsValid = true, InvalidFiles = new List<string>() };

        if (files.Count == 0)
            return result; // Không có ảnh thì OK

        foreach (var file in files)
        {
            // Kiểm tra content type
            if (!IsImageContentType(file.ContentType))
            {
                result.IsValid = false;
                result.InvalidFiles.Add($"{file.FileName} (không phải ảnh)");
                continue;
            }

            // Kiểm tra kích thước file
            if (file.Length > 5 * 1024 * 1024) // 5MB
            {
                result.IsValid = false;
                result.InvalidFiles.Add($"{file.FileName} (quá lớn, max 5MB)");
                continue;
            }

            // Kiểm tra file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                result.IsValid = false;
                result.InvalidFiles.Add($"{file.FileName} (định dạng không hỗ trợ, chỉ chấp nhận: {string.Join(", ", allowedExtensions)})");
                continue;
            }
        }

        return result;
    }

    private class ImageValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> InvalidFiles { get; set; } = new();
    }

    // Helper method để xử lý ảnh mới cho room (POST endpoint)
    private static async Task<RoomImageUpdateResponseDTO> ProcessNewRoomImagesAsync(
        Guid roomId, 
        IFormFileCollection files, 
        ICloudinaryService cloudinaryService, 
        HttpContext context)
    {
        var response = new RoomImageUpdateResponseDTO();
        var newImageUrls = new List<string>();
        var failedUploads = new List<string>();

        using var scope = context.RequestServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Xử lý NewImages (upload ảnh mới)
            if (files.Count > 0)
            {
                foreach (var file in files)
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

                        dbContext.Images.Add(image);
                        newImageUrls.Add(imageUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to upload {file.FileName}: {ex.Message}");
                        failedUploads.Add($"{file.FileName} (lỗi upload: {ex.Message})");
                    }
                }

                response.NewImageCount = newImageUrls.Count;
            }

            // Lưu tất cả thay đổi vào database
            await dbContext.SaveChangesAsync();

            // Cập nhật response
            response.NewImageUrls = newImageUrls;
            response.UploadedImageUrls = newImageUrls; // Backward compatibility
            response.FailedUploads = failedUploads;
            response.FailedImageCount = failedUploads.Count;
            response.SuccessImageCount = newImageUrls.Count; // Backward compatibility
            response.TotalImageCount = newImageUrls.Count;

            // Tạo message
            if (response.NewImageCount > 0)
            {
                response.Message = $"Upload thành công {response.NewImageCount} ảnh mới.";
            }
            else
            {
                response.Message = "Không có ảnh nào được upload.";
            }

            if (response.FailedImageCount > 0)
            {
                response.Message += $" {response.FailedImageCount} ảnh upload thất bại.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ProcessNewRoomImagesAsync: {ex.Message}");
            response.Message = $"Có lỗi xảy ra khi upload ảnh: {ex.Message}";
        }

        return response;
    }

    // Helper method để xử lý ảnh theo logic mới cho room (PUT endpoint)
    private static async Task<RoomImageUpdateResponseDTO> ProcessRoomImagesAsync(
        Guid roomId, 
        RoomImageUpdateDTO imageUpdateDto, 
        IFormFileCollection files, 
        ICloudinaryService cloudinaryService, 
        HttpContext context)
    {
        var response = new RoomImageUpdateResponseDTO();
        var existingImageUrls = new List<string>();
        var newImageUrls = new List<string>();
        var deletedImageUrls = new List<string>();
        var failedUploads = new List<string>();

        using var scope = context.RequestServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // 1. Lấy danh sách ảnh hiện tại của room
            var currentImages = await dbContext.Images
                .Where(img => img.TableType == TableTypeImage.RoomHotel && img.TypeID == roomId)
                .ToListAsync();

            // 2. Xử lý ExistingImages (giữ lại ảnh được chỉ định)
            if (imageUpdateDto.ExistingImageIds != null && imageUpdateDto.ExistingImageIds.Any())
            {
                var existingImages = currentImages
                    .Where(img => imageUpdateDto.ExistingImageIds.Contains(img.ImgID))
                    .ToList();
                
                existingImageUrls = existingImages.Select(img => img.URL).ToList();
                response.ExistingImageCount = existingImages.Count;
            }
            else
            {
                // Nếu không chỉ định ExistingImages, giữ tất cả ảnh cũ
                existingImageUrls = currentImages.Select(img => img.URL).ToList();
                response.ExistingImageCount = currentImages.Count;
            }

            // 3. Xử lý DeleteImages (xóa ảnh được chỉ định)
            if (imageUpdateDto.DeleteImageIds != null && imageUpdateDto.DeleteImageIds.Any())
            {
                var imagesToDelete = currentImages
                    .Where(img => imageUpdateDto.DeleteImageIds.Contains(img.ImgID))
                    .ToList();

                foreach (var image in imagesToDelete)
                {
                    try
                    {
                        // Xóa từ Cloudinary
                        var publicId = ExtractPublicIdFromUrl(image.URL);
                        if (!string.IsNullOrEmpty(publicId))
                        {
                            await cloudinaryService.DeleteImageAsync(publicId);
                        }

                        // Xóa từ database
                        dbContext.Images.Remove(image);
                        deletedImageUrls.Add(image.URL);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to delete image {image.URL}: {ex.Message}");
                        failedUploads.Add($"Xóa ảnh {image.URL} thất bại: {ex.Message}");
                    }
                }

                response.DeletedImageCount = deletedImageUrls.Count;
            }

            // 4. Xử lý NewImages (upload ảnh mới)
            if (files.Count > 0)
            {
                foreach (var file in files)
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

                        dbContext.Images.Add(image);
                        newImageUrls.Add(imageUrl);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to upload {file.FileName}: {ex.Message}");
                        failedUploads.Add($"{file.FileName} (lỗi upload: {ex.Message})");
                    }
                }

                response.NewImageCount = newImageUrls.Count;
            }

            // 5. Lưu tất cả thay đổi vào database
            await dbContext.SaveChangesAsync();

            // 6. Cập nhật response
            response.ExistingImageUrls = existingImageUrls;
            response.NewImageUrls = newImageUrls;
            response.UploadedImageUrls = newImageUrls; // Backward compatibility
            response.DeletedImageUrls = deletedImageUrls;
            response.FailedUploads = failedUploads;
            response.FailedImageCount = failedUploads.Count;
            response.SuccessImageCount = newImageUrls.Count; // Backward compatibility
            response.TotalImageCount = existingImageUrls.Count + newImageUrls.Count;

            // 7. Tạo message
            var messageParts = new List<string>();
            if (response.ExistingImageCount > 0)
                messageParts.Add($"Giữ lại {response.ExistingImageCount} ảnh cũ");
            if (response.NewImageCount > 0)
                messageParts.Add($"Thêm {response.NewImageCount} ảnh mới");
            if (response.DeletedImageCount > 0)
                messageParts.Add($"Xóa {response.DeletedImageCount} ảnh cũ");

            response.Message = messageParts.Any() 
                ? $"Cập nhật ảnh phòng thành công. {string.Join(", ", messageParts)}."
                : "Cập nhật ảnh phòng thành công (không có thay đổi).";

            if (response.FailedImageCount > 0)
            {
                response.Message += $" {response.FailedImageCount} ảnh xử lý thất bại.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ProcessRoomImagesAsync: {ex.Message}");
            response.Message = $"Có lỗi xảy ra khi xử lý ảnh: {ex.Message}";
        }

        return response;
    }

    // Helper method để extract public ID từ Cloudinary URL
    private static string ExtractPublicIdFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (segments.Length >= 2)
            {
                // Cloudinary URL format: /v1234567890/folder/public_id.extension
                var publicIdWithExtension = segments[^1]; // Lấy phần cuối cùng
                var publicId = Path.GetFileNameWithoutExtension(publicIdWithExtension);
                return publicId;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting public ID from URL {url}: {ex.Message}");
        }
        
        return string.Empty;
    }
}
