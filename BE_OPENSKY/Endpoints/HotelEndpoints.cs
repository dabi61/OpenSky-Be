using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints;

public static class HotelEndpoints
{
    public static void MapHotelEndpoints(this WebApplication app)
    {
        var hotelGroup = app.MapGroup("/hotels")
            .WithTags("Hotel")
            .WithOpenApi();

        // 1. Cập nhật thông tin khách sạn với ảnh
        hotelGroup.MapPut("/{hotelId:guid}", async (Guid hotelId, HttpContext context, [FromServices] IHotelService hotelService, [FromServices] ICloudinaryService cloudinaryService) =>
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

                // Khởi tạo UpdateHotelDTO
                var updateDto = new UpdateHotelDTO();

                // Xử lý multipart form data
                if (context.Request.HasFormContentType)
                {
                    try
                    {
                        var form = await context.Request.ReadFormAsync();
                        
                        // Lấy thông tin text từ form
                        if (form.ContainsKey("hotelName") && !string.IsNullOrWhiteSpace(form["hotelName"].FirstOrDefault()))
                            updateDto.HotelName = form["hotelName"].FirstOrDefault();
                        
                        if (form.ContainsKey("description"))
                            updateDto.Description = form["description"].FirstOrDefault();
                        
                        if (form.ContainsKey("address") && !string.IsNullOrWhiteSpace(form["address"].FirstOrDefault()))
                            updateDto.Address = form["address"].FirstOrDefault();
                        
                        if (form.ContainsKey("province") && !string.IsNullOrWhiteSpace(form["province"].FirstOrDefault()))
                            updateDto.Province = form["province"].FirstOrDefault();

                        if (form.ContainsKey("latitude") && decimal.TryParse(form["latitude"].FirstOrDefault(), out var latitude))
                        {
                            if (latitude >= -90 && latitude <= 90)
                                updateDto.Latitude = latitude;
                            else
                                return Results.BadRequest(new { message = "Vĩ độ phải từ -90 đến 90" });
                        }

                        if (form.ContainsKey("longitude") && decimal.TryParse(form["longitude"].FirstOrDefault(), out var longitude))
                        {
                            if (longitude >= -180 && longitude <= 180)
                                updateDto.Longitude = longitude;
                            else
                                return Results.BadRequest(new { message = "Kinh độ phải từ -180 đến 180" });
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
                            deletedImageUrls = await hotelService.DeleteHotelImagesAsync(hotelId, userId, imageAction);
                        }

                        // Cập nhật thông tin khách sạn
                        var success = await hotelService.UpdateHotelAsync(hotelId, userId, updateDto);
                        if (!success)
                        {
                            return Results.NotFound(new { message = "Không tìm thấy khách sạn hoặc bạn không có quyền cập nhật" });
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
                            message = $"Cập nhật thông tin khách sạn thành công. Thay thế {deletedImageUrls.Count} ảnh cũ với {uploadedImageUrls.Count} ảnh mới.";
                        }
                        else // keep
                        {
                            message = uploadedImageUrls.Count > 0 
                                ? $"Cập nhật thông tin khách sạn thành công. Thêm {uploadedImageUrls.Count} ảnh mới (giữ nguyên ảnh cũ)."
                                : "Cập nhật thông tin khách sạn thành công (không có ảnh mới).";
                        }

                        var response = new UpdateHotelWithImagesResponseDTO
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
                            var updateHotelDto = System.Text.Json.JsonSerializer.Deserialize<UpdateHotelDTO>(jsonString);
                            if (updateHotelDto == null)
                                return Results.BadRequest(new { message = "Dữ liệu JSON không hợp lệ" });
                            
                            updateDto = updateHotelDto;
                        }
                        else
                        {
                            return Results.BadRequest(new { message = "Request body không được để trống" });
                        }

                        // Cập nhật thông tin khách sạn
                        var success = await hotelService.UpdateHotelAsync(hotelId, userId, updateDto);
                        
                        return success 
                            ? Results.Ok(new { message = "Cập nhật thông tin khách sạn thành công" })
                            : Results.NotFound(new { message = "Không tìm thấy khách sạn hoặc bạn không có quyền cập nhật" });
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
                Console.WriteLine($"Error updating hotel with images: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật thông tin khách sạn: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateHotelWithImages")
        .WithSummary("Cập nhật thông tin khách sạn với ảnh")
        .WithDescription("Cập nhật thông tin khách sạn với ảnh. imageAction: files. keep: Giữ ảnh cũ + thêm ảnh mới • replace: Xóa ảnh cũ + thay thế bằng ảnh mới")
        .WithOpenApi(operation => new Microsoft.OpenApi.Models.OpenApiOperation(operation)
        {
            Parameters = new List<Microsoft.OpenApi.Models.OpenApiParameter>
            {
                new Microsoft.OpenApi.Models.OpenApiParameter
                {
                    Name = "hotelId",
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
                                ["hotelName"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tên khách sạn"
                                },
                                ["description"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Mô tả khách sạn"
                                },
                                ["address"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Địa chỉ khách sạn"
                                },
                                ["province"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tỉnh/Thành phố"
                                },
                                ["latitude"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "number",
                                    Format = "decimal",
                                    Description = "Vĩ độ (-90 đến 90)"
                                },
                                ["longitude"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "number",
                                    Format = "decimal",
                                    Description = "Kinh độ (-180 đến 180)"
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
                                    Description = "Danh sách ảnh khách sạn mới (JPEG, PNG, GIF, WebP, max 5MB/file)"
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
                                ["hotelName"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tên khách sạn"
                                },
                                ["description"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Mô tả khách sạn"
                                },
                                ["address"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Địa chỉ khách sạn"
                                },
                                ["province"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tỉnh/Thành phố"
                                },
                                ["latitude"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "number",
                                    Format = "decimal",
                                    Description = "Vĩ độ (-90 đến 90)"
                                },
                                ["longitude"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "number",
                                    Format = "decimal",
                                    Description = "Kinh độ (-180 đến 180)"
                                }
                            }
                        }
                    }
                }
            }
        })
        .Produces<UpdateHotelWithImagesResponseDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("HotelOnly");



        // 2. Tìm kiếm và lọc khách sạn (Public - không cần auth)
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

        // 3. Customer đăng ký mở khách sạn với ảnh (chuyển từ Customer -> Hotel sau khi được duyệt)
        hotelGroup.MapPost("/apply", async (HttpContext context, [FromServices] IHotelService hotelService, [FromServices] ICloudinaryService cloudinaryService) =>
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

                // Kiểm tra content type
                if (!context.Request.HasFormContentType)
                {
                    return Results.BadRequest(new { message = "Request phải là multipart/form-data để upload ảnh cùng lúc" });
                }

                // Đọc form data
                var form = await context.Request.ReadFormAsync();
                
                // Validate và parse hotel data
                if (!form.TryGetValue("hotelName", out var hotelNameValue) || string.IsNullOrWhiteSpace(hotelNameValue))
                    return Results.BadRequest(new { message = "Tên khách sạn không được để trống" });

                if (!form.TryGetValue("address", out var addressValue) || string.IsNullOrWhiteSpace(addressValue))
                    return Results.BadRequest(new { message = "Địa chỉ không được để trống" });

                if (!form.TryGetValue("province", out var provinceValue) || string.IsNullOrWhiteSpace(provinceValue))
                    return Results.BadRequest(new { message = "Tỉnh/Thành phố không được để trống" });

                if (!decimal.TryParse(form["latitude"], out var latitude))
                    return Results.BadRequest(new { message = "Vĩ độ không hợp lệ" });

                if (!decimal.TryParse(form["longitude"], out var longitude))
                    return Results.BadRequest(new { message = "Kinh độ không hợp lệ" });

                // Remove star validation for POST
                // if (!int.TryParse(form["star"], out var star))
                //     return Results.BadRequest(new { message = "Số sao không hợp lệ" });

                // Validate ranges
                if (latitude < -90 || latitude > 90)
                    return Results.BadRequest(new { message = "Vĩ độ phải từ -90 đến 90" });

                if (longitude < -180 || longitude > 180)
                    return Results.BadRequest(new { message = "Kinh độ phải từ -180 đến 180" });

                // if (star < 1 || star > 5)
                //     return Results.BadRequest(new { message = "Số sao phải từ 1 đến 5" });

                // Tạo DTO cho hotel application
                var applicationDto = new HotelApplicationDTO
                {
                    HotelName = hotelNameValue.ToString().Trim(),
                    Address = addressValue.ToString().Trim(),
                    Province = provinceValue.ToString().Trim(),
                    Latitude = latitude,
                    Longitude = longitude,

                    Description = form["description"].ToString(),
                };

                // Tạo đơn đăng ký khách sạn
                var hotelId = await hotelService.CreateHotelApplicationAsync(userId, applicationDto);

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

                            uploadedImageUrls.Add(imageUrl);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to upload {file.FileName}: {ex.Message}");
                            failedUploads.Add($"{file.FileName} (lỗi upload: {ex.Message})");
                        }
                    }
                }
                
                var response = new HotelApplicationWithImagesResponseDTO
                {
                    HotelID = hotelId,
                    Message = uploadedImageUrls.Count > 0 
                        ? $"Đơn đăng ký khách sạn đã được gửi thành công với {uploadedImageUrls.Count} ảnh. Vui lòng chờ Admin duyệt."
                        : "Đơn đăng ký khách sạn đã được gửi thành công (không có ảnh). Vui lòng chờ Admin duyệt.",
                    UploadedImageUrls = uploadedImageUrls,
                    FailedUploads = failedUploads,
                    SuccessImageCount = uploadedImageUrls.Count,
                    FailedImageCount = failedUploads.Count,
                    Status = "Inactive"
                };

                return Results.Created($"/hotels/{hotelId}", response);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating hotel application with images: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi gửi đơn đăng ký khách sạn: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("ApplyHotelWithImages")
        .WithSummary("Customer đăng ký mở khách sạn với ảnh")
        .WithDescription("Customer có thể đăng ký để trở thành Hotel và upload ảnh cùng lúc. Sử dụng multipart/form-data với fields: hotelName, address, province, latitude, longitude, description và files")
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
                                ["hotelName"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tên khách sạn"
                                },
                                ["description"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Mô tả khách sạn (tùy chọn)"
                                },
                                ["address"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Địa chỉ khách sạn"
                                },
                                ["province"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tỉnh/Thành phố"
                                },
                                ["latitude"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "number",
                                    Format = "decimal",
                                    Description = "Vĩ độ (-90 đến 90)"
                                },
                                ["longitude"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "number",
                                    Format = "decimal",
                                    Description = "Kinh độ (-180 đến 180)"
                                },
                                ["files"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "array",
                                    Items = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    },
                                    Description = "Danh sách ảnh khách sạn (JPEG, PNG, GIF, WebP, max 5MB/file)"
                                }
                            },
                            Required = new HashSet<string> { "hotelName", "address", "province", "latitude", "longitude" }
                        }
                    }
                }
            }
        })
        .Produces<HotelApplicationWithImagesResponseDTO>(201)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("CustomerOnly");

        // 4. Xem tất cả khách sạn đang hoạt động (Public)
        hotelGroup.MapGet("/active", async ([FromServices] IHotelService hotelService, HttpContext context, int page = 1, int limit = 10) =>
        {
            try
            {
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var activeHotels = await hotelService.GetHotelsByStatusAsync(HotelStatus.Active, page, limit);
                return Results.Ok(activeHotels);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy danh sách khách sạn đang hoạt động: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetActiveHotels")
        .WithSummary("Xem tất cả khách sạn đang hoạt động")
        .WithDescription("Mọi người đều có thể xem danh sách khách sạn có status Active với phân trang")
        .Produces<PaginatedHotelsResponseDTO>(200)
        .AllowAnonymous();

        // 5. Lấy tất cả hotel (không bao gồm status Removed) - Public
        hotelGroup.MapGet("/all", async ([FromServices] IHotelService hotelService, int page = 1, int limit = 10) =>
        {
            try
            {
                // Validate parameters
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var result = await hotelService.GetHotelsExcludeRemovedAsync(page, limit);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy danh sách khách sạn: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetAllHotelsExcludeRemoved")
        .WithSummary("Lấy tất cả khách sạn (không bao gồm khách sạn đã xóa)")
        .WithDescription("Lấy danh sách tất cả khách sạn, loại trừ những khách sạn có status Removed")
        .Produces<PaginatedHotelsResponseDTO>(200)
        .Produces(400)
        .Produces(500)
        .AllowAnonymous();

        // 6. Xem chi tiết khách sạn bằng ID (Public)
        hotelGroup.MapGet("/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                var hotelDetail = await hotelService.GetHotelDetailAsync(hotelId);
                
                return hotelDetail != null 
                    ? Results.Ok(hotelDetail)
                    : Results.NotFound(new { message = "Không tìm thấy khách sạn" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy chi tiết khách sạn: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelDetail")
        .WithSummary("Xem chi tiết khách sạn")
        .WithDescription("Mọi người đều có thể xem chi tiết khách sạn bao gồm ảnh (hiển thị tất cả khách sạn)")
        .Produces<HotelDetailResponseDTO>(200)
        .Produces(404)
        .AllowAnonymous();

        // 7. Lấy danh sách khách sạn theo số sao (Public)
        hotelGroup.MapGet("/star/{star}", async (int star, [FromServices] IHotelService hotelService, HttpContext context, int page = 1, int size = 10) =>
        {
            try
            {
                // Validate star parameter
                if (star < 1 || star > 5)
                {
                    return Results.BadRequest(new { message = "Số sao phải từ 1 đến 5" });
                }

                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                var paginatedHotels = await hotelService.GetHotelsByStarAsync(star, page, size);
                
                return Results.Ok(paginatedHotels);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy danh sách khách sạn theo sao: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelsByStar")
        .WithSummary("Lấy danh sách khách sạn theo số sao")
        .WithDescription("Mọi người đều có thể lấy danh sách khách sạn theo số sao (1-5) với phân trang (chỉ hiển thị khách sạn Active)")
        .Produces<PaginatedHotelsResponseDTO>(200)
        .Produces(400)
        .AllowAnonymous();

        // 8. Lấy danh sách khách sạn theo tỉnh/thành phố (Public)
        hotelGroup.MapGet("/province/{province}", async (string province, [FromServices] IHotelService hotelService, HttpContext context, int page = 1, int size = 10) =>
        {
            try
            {
                // Validate province parameter
                if (string.IsNullOrWhiteSpace(province))
                {
                    return Results.BadRequest(new { message = "Tỉnh/thành phố không được để trống" });
                }

                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                var paginatedHotels = await hotelService.GetHotelsByProvinceAsync(province, page, size);
                
                return Results.Ok(paginatedHotels);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy danh sách khách sạn theo tỉnh: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelsByProvince")
        .WithSummary("Lấy danh sách khách sạn theo tỉnh/thành phố")
        .WithDescription("Mọi người đều có thể lấy danh sách khách sạn theo tỉnh/thành phố với phân trang (chỉ hiển thị khách sạn Active)")
        .Produces<PaginatedHotelsResponseDTO>(200)
        .Produces(400)
        .AllowAnonymous();

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

        // 8. Admin từ chối đơn đăng ký khách sạn
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

        // 9. Customer xem đơn đăng ký khách sạn của mình
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

        // 10. Lấy danh sách khách sạn theo status (ADMIN - SUPERVISOR)
        hotelGroup.MapGet("/status/{status}", async (HotelStatus status, [FromServices] IHotelService hotelService, HttpContext context, int page = 1, int size = 10) =>
        {
            try
            {
                // Kiểm tra quyền Admin hoặc Supervisor
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được xem danh sách khách sạn theo status." }, statusCode: 403);
                }

                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                var paginatedHotels = await hotelService.GetHotelsByStatusAsync(status, page, size);
                
                return Results.Ok(paginatedHotels);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy danh sách khách sạn theo status: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelsByStatus")
        .WithSummary("Lấy danh sách khách sạn theo status")
        .WithDescription("Admin và Supervisor có thể lấy danh sách khách sạn theo trạng thái (Active, Inactive, Suspend, Removed) với phân trang")
        .Produces<PaginatedHotelsResponseDTO>(200)
        .Produces(403)
        .RequireAuthorization("SupervisorOrAdmin");

        // 11. Cập nhật status khách sạn (ADMIN - SUPERVISOR)
        hotelGroup.MapPut("/{hotelId:guid}/status", async (Guid hotelId, [FromBody] UpdateHotelStatusDTO updateStatusDto, [FromServices] IHotelService hotelService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Admin hoặc Supervisor
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được cập nhật status khách sạn." }, statusCode: 403);
                }

                var success = await hotelService.UpdateHotelStatusAsync(hotelId, updateStatusDto.Status);
                
                if (!success)
                {
                    return Results.NotFound(new { message = "Không tìm thấy khách sạn hoặc status không hợp lệ" });
                }

                return Results.Ok(new { message = "Cập nhật status khách sạn thành công", status = updateStatusDto.Status });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật status khách sạn: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateHotelStatus")
        .WithSummary("Cập nhật status khách sạn")
        .WithDescription("Admin và Supervisor có thể cập nhật trạng thái khách sạn (Active, Inactive, Suspend, Removed)")
        .Produces(200)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("SupervisorOrAdmin");
        }

        private static bool IsImageContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;
                
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }
    }