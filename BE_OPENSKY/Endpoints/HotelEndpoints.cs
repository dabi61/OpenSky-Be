using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints;

public static class HotelEndpoints
{
    public static void MapHotelEndpoints(this WebApplication app)
    {
        var hotelGroup = app.MapGroup("/hotels")
            .WithTags("Hotel")
            .WithOpenApi();

        // 2. Tìm kiếm khách sạn (Public - không cần auth)
        hotelGroup.MapGet("/search", async (
            IHotelService hotelService,
            HttpContext context,
            string? keyword = null) =>
        {
            try
            {
                // Lấy raw query string để validate
                var queryPage = context.Request.Query["page"].ToString();
                var queryLimit = context.Request.Query["limit"].ToString();

                int? page = null;
                int? limit = null;

                // Nếu có truyền limit nhưng không truyền page
                if (!string.IsNullOrEmpty(queryLimit) && string.IsNullOrEmpty(queryPage))
                {
                    return Results.BadRequest(new { message = "page không được để trống" });
                }

                // Nếu có truyền page nhưng không truyền limit
                if (!string.IsNullOrEmpty(queryPage) && string.IsNullOrEmpty(queryLimit))
                {
                    return Results.BadRequest(new { message = "limit ko được để trống" });
                }

                // Validate page nếu có truyền
                if (!string.IsNullOrEmpty(queryPage))
                {
                    if (!int.TryParse(queryPage, out var pageValue) || pageValue < 1)
                    {
                        return Results.BadRequest(new { message = "sai định dạng" });
                    }
                    page = pageValue;
                }

                // Validate limit nếu có truyền
                if (!string.IsNullOrEmpty(queryLimit))
                {
                    if (!int.TryParse(queryLimit, out var limitValue) || limitValue < 1 || limitValue > 100)
                    {
                        return Results.BadRequest(new { message = "sai định dạng" });
                    }
                    limit = limitValue;
                }

                var searchDto = new HotelSearchDTO
                {
                    Keyword = keyword,
                    Page = page ?? 1,
                    Limit = limit ?? 10
                };

                var result = await hotelService.SearchHotelsAsync(searchDto);

                // Kiểm tra nếu không tìm thấy kết quả
                if (result.TotalCount == 0)
                {
                    return Results.Ok(new
                    {
                        message = "Không tìm thấy khách sạn nào",
                        hotels = new List<object>(),
                        totalCount = 0,
                        page = searchDto.Page,
                        limit = searchDto.Limit,
                        totalPages = 0,
                        hasNextPage = false,
                        hasPreviousPage = false
                    });
                }

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
        .WithSummary("Tìm kiếm khách sạn")
        .WithDescription("Tìm kiếm khách sạn theo tên (keyword). Hỗ trợ phân trang. Chỉ hiển thị khách sạn Active.")
        .WithOpenApi(operation => new(operation)
        {
            Summary = "Tìm kiếm khách sạn",
            Description = "Tìm kiếm khách sạn theo tên (keyword). Hỗ trợ phân trang. Chỉ hiển thị khách sạn Active.",
            Parameters = new List<OpenApiParameter>
            {
                new() { Name = "keyword", In = ParameterLocation.Query, Description = "Tìm kiếm theo tên khách sạn (không phân biệt chữ hoa/thường)", Required = false, Schema = new() { Type = "string" } },
                new() { Name = "page", In = ParameterLocation.Query, Description = "Số trang (mặc định: 1)", Required = false, Schema = new() { Type = "integer", Minimum = 1 } },
                new() { Name = "limit", In = ParameterLocation.Query, Description = "Số kết quả mỗi trang (mặc định: 10, tối đa: 100)", Required = false, Schema = new() { Type = "integer", Minimum = 1, Maximum = 100 } }
            }
        })
        .Produces<HotelSearchResponseDTO>(200)
        .Produces(500)
        .AllowAnonymous(); // Public endpoint - không cần authentication

        // 2.4. Lấy danh sách tỉnh/thành phố
        hotelGroup.MapGet("/provinces", async (IHotelService hotelService) =>
        {
            try
            {
                var provinces = await hotelService.GetHotelProvincesAsync();
                return Results.Ok(new
                {
                    provinces = provinces.OrderBy(p => p).ToList(),
                    totalCount = provinces.Length
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy danh sách tỉnh/thành phố: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetHotelProvinces") // đổi tên cho đúng service
        .WithSummary("Lấy danh sách tỉnh/thành phố của khách sạn")
        .WithDescription("Lấy danh sách tất cả các tỉnh/thành phố từ bảng Hotels")
        .Produces<object>(200)
        .Produces(500)
        .AllowAnonymous();



        // 2.5. Admin/Supervisor tìm kiếm hotel theo status với keyword
        hotelGroup.MapGet("/admin/search", async ([FromServices] IHotelService hotelService, HttpContext context, string? keyword = null, string? status = null, int page = 1, int size = 10) =>
        {
            try
            {
                // Kiểm tra quyền Admin hoặc Supervisor
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được tìm kiếm hotel theo status." }, statusCode: 403);
                }

                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                // Parse status (nếu có)
                HotelStatus? hotelStatus = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse<HotelStatus>(status, true, out var parsedStatus))
                    {
                        hotelStatus = parsedStatus;
                    }
                    else
                    {
                        return Results.BadRequest(new { message = "Status không hợp lệ. Các giá trị hợp lệ: Active, Inactive, Suspend, Removed" });
                    }
                }

                var searchDto = new AdminHotelSearchDTO
                {
                    Keyword = keyword,
                    Status = hotelStatus,
                    Page = page,
                    Size = size
                };

                var result = await hotelService.SearchHotelsForAdminAsync(searchDto);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi tìm kiếm hotel: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("AdminSearchHotels")
        .WithSummary("Admin/Supervisor tìm kiếm hotel theo status với keyword")
        .WithDescription("Admin và Supervisor có thể tìm kiếm hotel theo tên (keyword) và lọc theo status. Nếu không truyền status, sẽ lấy tất cả hotel trừ Removed (Active + Inactive + Suspend).")
        .Produces<HotelSearchResponseDTO>(200)
        .Produces(400)
        .Produces(403)
        .RequireAuthorization("SupervisorOrAdmin");

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

                // Validate hotelName với regex (hỗ trợ tiếng Việt có dấu)
                var hotelNameRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,255}$");
                if (!hotelNameRegex.IsMatch(hotelNameValue.ToString()))
                    return Results.BadRequest(new { message = "Tên khách sạn chứa ký tự không hợp lệ" });

                if (!form.TryGetValue("address", out var addressValue) || string.IsNullOrWhiteSpace(addressValue))
                    return Results.BadRequest(new { message = "Địa chỉ không được để trống" });

                // Validate address với regex (hỗ trợ tiếng Việt có dấu)
                var addressRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,255}$");
                if (!addressRegex.IsMatch(addressValue.ToString()))
                    return Results.BadRequest(new { message = "Địa chỉ chứa ký tự không hợp lệ" });

                if (!form.TryGetValue("province", out var provinceValue) || string.IsNullOrWhiteSpace(provinceValue))
                    return Results.BadRequest(new { message = "Tỉnh/Thành phố không được để trống" });

                // Validate province
                var provinceTrimmed = provinceValue.ToString().Trim();
                if (!ProvinceConstants.IsValidProvince(provinceTrimmed))
                    return Results.BadRequest(new { message = "Tỉnh không hợp lệ" });

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

                // Validate description nếu có
                var descriptionValue = form["description"].ToString();
                if (!string.IsNullOrWhiteSpace(descriptionValue))
                {
                    var descriptionRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,5000}$");
                    if (!descriptionRegex.IsMatch(descriptionValue))
                        return Results.BadRequest(new { message = "Mô tả chứa ký tự không hợp lệ" });
                }

                // Tạo DTO cho hotel application
                var applicationDto = new HotelApplicationDTO
                {
                    HotelName = hotelNameValue.ToString().Trim(),
                    Address = addressValue.ToString().Trim(),
                    Province = provinceTrimmed,
                    Latitude = latitude,
                    Longitude = longitude,

                    Description = descriptionValue,
                };

                // Tạo đơn đăng ký khách sạn
                // Kiểm tra ảnh trước khi tạo hotel
                var imageValidationResult = ValidateHotelImages(form.Files);
                if (!imageValidationResult.IsValid)
                {
                    return Results.BadRequest(new { 
                        message = "Có ảnh không hợp lệ", 
                        invalidFiles = imageValidationResult.InvalidFiles 
                    });
                }

                var hotelId = await hotelService.CreateHotelApplicationAsync(userId, applicationDto);

                // Xử lý upload ảnh mới (NewImages) - sử dụng logic mới
                var imageResponse = await ProcessNewHotelImagesAsync(hotelId, form.Files, cloudinaryService, context);
                
                var response = new HotelApplicationWithImagesResponseDTO
                {
                    HotelID = hotelId,
                    Message = imageResponse.NewImageCount > 0 
                        ? $"Đơn đăng ký khách sạn đã được gửi thành công với {imageResponse.NewImageCount} ảnh. Vui lòng chờ Admin duyệt."
                        : "Đơn đăng ký khách sạn đã được gửi thành công (không có ảnh). Vui lòng chờ Admin duyệt.",
                    UploadedImageUrls = imageResponse.NewImageUrls,
                    FailedUploads = imageResponse.FailedUploads,
                    SuccessImageCount = imageResponse.NewImageCount,
                    FailedImageCount = imageResponse.FailedImageCount,
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
        hotelGroup.MapPut("/reject/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context) =>
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

        // 12. Cập nhật ảnh khách sạn theo cách mới (ExistingImages, NewImages, DeleteImages)
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

                // Cho phép chủ sở hữu khách sạn cập nhật (không cần role Hotel), miễn không phải Removed
                var isOwner = await hotelService.IsHotelOwnerAsync(hotelId, userId);
                if (!isOwner)
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Khởi tạo HotelImageUpdateDTO
                var imageUpdateDto = new HotelImageUpdateDTO { HotelId = hotelId };

                // Xử lý multipart form data
                if (context.Request.HasFormContentType)
                {
                    try
                    {
                        var form = await context.Request.ReadFormAsync();
                        
                        // Lấy thông tin text từ form
                        if (form.ContainsKey("hotelName") && !string.IsNullOrWhiteSpace(form["hotelName"].FirstOrDefault()))
                        {
                            var hotelNameValue = form["hotelName"].FirstOrDefault();
                            // Validate hotelName với regex (hỗ trợ tiếng Việt có dấu)
                            var hotelNameRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,255}$");
                            if (!hotelNameRegex.IsMatch(hotelNameValue))
                                return Results.BadRequest(new { message = "Tên khách sạn chứa ký tự không hợp lệ" });
                            
                            imageUpdateDto.HotelName = hotelNameValue;
                        }
                        
                        if (form.ContainsKey("description") && !string.IsNullOrWhiteSpace(form["description"].FirstOrDefault()))
                        {
                            var descriptionValue = form["description"].FirstOrDefault();
                            // Validate description với regex (hỗ trợ tiếng Việt có dấu)
                            var descriptionRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,5000}$");
                            if (!descriptionRegex.IsMatch(descriptionValue))
                                return Results.BadRequest(new { message = "Mô tả chứa ký tự không hợp lệ" });
                            
                            imageUpdateDto.Description = descriptionValue;
                        }
                        
                        if (form.ContainsKey("address") && !string.IsNullOrWhiteSpace(form["address"].FirstOrDefault()))
                        {
                            var addressValue = form["address"].FirstOrDefault();
                            // Validate address với regex (hỗ trợ tiếng Việt có dấu)
                            var addressRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,255}$");
                            if (!addressRegex.IsMatch(addressValue))
                                return Results.BadRequest(new { message = "Địa chỉ chứa ký tự không hợp lệ" });
                            
                            imageUpdateDto.Address = addressValue;
                        }
                        
                        if (form.ContainsKey("province") && !string.IsNullOrWhiteSpace(form["province"].FirstOrDefault()))
                        {
                            var provinceValue = form["province"].FirstOrDefault().Trim();
                            // Validate province
                            if (!ProvinceConstants.IsValidProvince(provinceValue))
                                return Results.BadRequest(new { message = "Tỉnh không hợp lệ" });
                            
                            imageUpdateDto.Province = provinceValue;
                        }

                        if (form.ContainsKey("latitude") && decimal.TryParse(form["latitude"].FirstOrDefault(), out var latitude))
                        {
                            if (latitude >= -90 && latitude <= 90)
                                imageUpdateDto.Latitude = latitude;
                            else
                                return Results.BadRequest(new { message = "Vĩ độ phải từ -90 đến 90" });
                        }

                        if (form.ContainsKey("longitude") && decimal.TryParse(form["longitude"].FirstOrDefault(), out var longitude))
                        {
                            if (longitude >= -180 && longitude <= 180)
                                imageUpdateDto.Longitude = longitude;
                            else
                                return Results.BadRequest(new { message = "Kinh độ phải từ -180 đến 180" });
                        }

                        // Lấy ExistingImageIds (IDs của ảnh muốn giữ lại)
                        if (form.ContainsKey("existingImageIds"))
                        {
                            var existingIdsString = form["existingImageIds"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(existingIdsString))
                            {
                                var existingIds = existingIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Where(id => int.TryParse(id.Trim(), out _))
                                    .Select(int.Parse)
                                    .ToList();
                                imageUpdateDto.ExistingImageIds = existingIds;
                            }
                        }

                        // Lấy DeleteImageIds (IDs của ảnh muốn xóa)
                        if (form.ContainsKey("deleteImageIds"))
                        {
                            var deleteIdsString = form["deleteImageIds"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(deleteIdsString))
                            {
                                var deleteIds = deleteIdsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Where(id => int.TryParse(id.Trim(), out _))
                                    .Select(int.Parse)
                                    .ToList();
                                imageUpdateDto.DeleteImageIds = deleteIds;
                            }
                        }

                        // Kiểm tra ảnh trước khi cập nhật hotel
                        var imageValidationResult = ValidateHotelImages(form.Files);
                        if (!imageValidationResult.IsValid)
                        {
                            return Results.BadRequest(new { 
                                message = "Có ảnh không hợp lệ", 
                                invalidFiles = imageValidationResult.InvalidFiles 
                            });
                        }

                        // Cập nhật thông tin khách sạn (nếu có)
                        if (!string.IsNullOrEmpty(imageUpdateDto.HotelName) || 
                            !string.IsNullOrEmpty(imageUpdateDto.Description) || 
                            !string.IsNullOrEmpty(imageUpdateDto.Address) || 
                            !string.IsNullOrEmpty(imageUpdateDto.Province) ||
                            imageUpdateDto.Latitude.HasValue || 
                            imageUpdateDto.Longitude.HasValue)
                        {
                            var updateDto = new UpdateHotelDTO
                            {
                                HotelName = imageUpdateDto.HotelName,
                                Description = imageUpdateDto.Description,
                                Address = imageUpdateDto.Address,
                                Province = imageUpdateDto.Province,
                                Latitude = imageUpdateDto.Latitude,
                                Longitude = imageUpdateDto.Longitude,
                                Status = imageUpdateDto.Status,
                                
                            };

                            var success = await hotelService.UpdateHotelAsync(hotelId, userId, updateDto);
                            if (!success)
                            {
                                return Results.NotFound(new { message = "Không tìm thấy khách sạn hoặc bạn không có quyền cập nhật" });
                            }
                        }

                        // Xử lý ảnh theo logic mới
                        var response = await ProcessHotelImagesAsync(hotelId, imageUpdateDto, form.Files, cloudinaryService, context);

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
                    return Results.BadRequest(new { message = "Chỉ hỗ trợ multipart/form-data cho endpoint này" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating hotel images: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật ảnh khách sạn: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateHotelImages")
        .WithSummary("Cập nhật ảnh khách sạn theo cách mới")
        .WithDescription("Cập nhật ảnh khách sạn với logic: ExistingImages (giữ lại), NewImages (thêm mới), DeleteImages (xóa bỏ)")
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
                                ["hotelName"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
                                ["description"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
                                ["address"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
                                ["province"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
                                ["latitude"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "number", Format = "decimal" },
                                ["longitude"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "number", Format = "decimal" },
                                ["existingImageIds"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Description = "IDs của ảnh muốn giữ lại (phân cách bằng dấu phẩy)" },
                                ["deleteImageIds"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Description = "IDs của ảnh muốn xóa (phân cách bằng dấu phẩy)" },
                                ["files"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "array", Items = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "binary" }, Description = "Ảnh mới cần upload" }
                            }
                        }
                    }
                }
            }
        })
        .Produces<HotelImageUpdateResponseDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .Produces(500)
        .RequireAuthorization("HotelOnly");
        }

        private static bool IsImageContentType(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;
                
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        private static ImageValidationResult ValidateHotelImages(IFormFileCollection files)
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

        // Helper method để xử lý ảnh theo logic mới
        private static async Task<HotelImageUpdateResponseDTO> ProcessHotelImagesAsync(
            Guid hotelId, 
            HotelImageUpdateDTO imageUpdateDto, 
            IFormFileCollection files, 
            ICloudinaryService cloudinaryService, 
            HttpContext context)
        {
            var response = new HotelImageUpdateResponseDTO();
            var existingImageUrls = new List<string>();
            var newImageUrls = new List<string>();
            var deletedImageUrls = new List<string>();
            var failedUploads = new List<string>();

            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // 1. Lấy danh sách ảnh hiện tại của hotel
                var currentImages = await dbContext.Images
                    .Where(img => img.TableType == TableTypeImage.Hotel && img.TypeID == hotelId)
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
                            var imageUrl = await cloudinaryService.UploadImageAsync(file, "hotels");
                            
                            // Lưu vào database
                            var image = new Image
                            {
                                TableType = TableTypeImage.Hotel,
                                TypeID = hotelId,
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
                response.DeletedImageUrls = deletedImageUrls;
                response.FailedUploads = failedUploads;
                response.FailedImageCount = failedUploads.Count;
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
                    ? $"Cập nhật ảnh khách sạn thành công. {string.Join(", ", messageParts)}."
                    : "Cập nhật ảnh khách sạn thành công (không có thay đổi).";

                if (response.FailedImageCount > 0)
                {
                    response.Message += $" {response.FailedImageCount} ảnh xử lý thất bại.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessHotelImagesAsync: {ex.Message}");
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

        // Helper method để xử lý ảnh mới cho hotel (POST endpoint)
        private static async Task<HotelImageUpdateResponseDTO> ProcessNewHotelImagesAsync(
            Guid hotelId, 
            IFormFileCollection files, 
            ICloudinaryService cloudinaryService, 
            HttpContext context)
        {
            var response = new HotelImageUpdateResponseDTO();
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
                            var imageUrl = await cloudinaryService.UploadImageAsync(file, "hotels");
                            
                            // Lưu vào database
                            var image = new Image
                            {
                                TableType = TableTypeImage.Hotel,
                                TypeID = hotelId,
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
                response.FailedUploads = failedUploads;
                response.FailedImageCount = failedUploads.Count;
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
                Console.WriteLine($"Error in ProcessNewHotelImagesAsync: {ex.Message}");
                response.Message = $"Có lỗi xảy ra khi upload ảnh: {ex.Message}";
            }

            return response;
        }
    }