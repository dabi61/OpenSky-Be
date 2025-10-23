using Microsoft.AspNetCore.Authorization;
using BE_OPENSKY.Models;
using TourStatus = BE_OPENSKY.Models.TourStatus;

namespace BE_OPENSKY.Endpoints;

public static class TourEndpoints
{
    public static void MapTourEndpoints(this WebApplication app)
    {
        var tourGroup = app.MapGroup("/tours")
            .WithTags("Tour")
            .WithOpenApi();

        // 1. Tạo tour mới với ảnh
        tourGroup.MapPost("/", async (HttpContext context, [FromServices] ITourService tourService, [FromServices] ICloudinaryService cloudinaryService) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Admin hoặc Supervisor
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được quản lý tour." }, statusCode: 403);
                }

                // Kiểm tra content type
                if (!context.Request.HasFormContentType)
                {
                    return Results.BadRequest(new { message = "Request phải là multipart/form-data để upload ảnh cùng lúc" });
                }

                // Đọc form data
                var form = await context.Request.ReadFormAsync();
                
                // Validate và parse tour data
                if (!form.TryGetValue("tourName", out var tourNameValue) || string.IsNullOrWhiteSpace(tourNameValue))
                    return Results.BadRequest(new { message = "Tên tour không được để trống" });

                // Validate tourName với regex (hỗ trợ tiếng Việt có dấu và các ký tự đặc biệt)
                // Sử dụng regex đơn giản hơn để tránh vấn đề với ký tự đặc biệt
                var tourNameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀ-ỹ0-9\s,./\-–—()&]{1,255}$");
                if (!tourNameRegex.IsMatch(tourNameValue.ToString()))
                    return Results.BadRequest(new { message = "Tên tour chứa ký tự không hợp lệ" });

                if (!form.TryGetValue("address", out var addressValue) || string.IsNullOrWhiteSpace(addressValue))
                    return Results.BadRequest(new { message = "Địa chỉ không được để trống" });

                // Validate address với regex (hỗ trợ tiếng Việt có dấu và các ký tự đặc biệt)
                var addressRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀ-ỹ0-9\s,./\-–—()&]{1,255}$");
                if (!addressRegex.IsMatch(addressValue.ToString()))
                    return Results.BadRequest(new { message = "Địa chỉ chứa ký tự không hợp lệ" });

                if (!form.TryGetValue("province", out var provinceValue) || string.IsNullOrWhiteSpace(provinceValue))
                    return Results.BadRequest(new { message = "Tỉnh/Thành phố không được để trống" });

                // Validate province
                var provinceTrimmed = provinceValue.ToString().Trim();
                
                if (!ProvinceConstants.IsValidProvince(provinceTrimmed))
                    return Results.BadRequest(new { message = "Tỉnh không hợp lệ" });

                // Remove star validation
                // if (!int.TryParse(form["star"], out var star) || star < 1 || star > 5)
                //     return Results.BadRequest(new { message = "Số sao phải từ 1 đến 5" });

                if (!decimal.TryParse(form["price"], out var price) || price <= 0)
                    return Results.BadRequest(new { message = "Giá tour phải lớn hơn 0" });

                if (!int.TryParse(form["maxPeople"], out var maxPeople) || maxPeople < 1 || maxPeople > 100)
                    return Results.BadRequest(new { message = "Số người tối đa phải từ 1 đến 100" });

                // Kiểm tra ảnh trước khi tạo tour
                var imageValidationResult = ValidateTourImages(form.Files);
                if (!imageValidationResult.IsValid)
                {
                    return Results.BadRequest(new { 
                        message = "Có ảnh không hợp lệ", 
                        invalidFiles = imageValidationResult.InvalidFiles 
                    });
                }

                // Validate description nếu có
                var descriptionValue = form["description"].ToString();
                if (!string.IsNullOrWhiteSpace(descriptionValue))
                {
                    var descriptionRegex = new System.Text.RegularExpressions.Regex(@"^[\p{L}0-9\s,./-]{1,5000}$");
                    if (!descriptionRegex.IsMatch(descriptionValue))
                        return Results.BadRequest(new { message = "Mô tả chứa ký tự không hợp lệ" });
                }

                // Tạo DTO cho tour
                var createTourDto = new CreateTourDTO
                {
                    TourName = tourNameValue.ToString().Trim(),
                    Description = descriptionValue,
                    Address = addressValue.ToString().Trim(),
                    Province = provinceTrimmed,
                    Price = price,
                    MaxPeople = maxPeople
                };

                // Tạo tour mới
                var tourId = await tourService.CreateTourAsync(userId, createTourDto);

                // Xử lý upload ảnh mới (NewImages) - sử dụng logic mới
                var imageResponse = await ProcessNewTourImagesAsync(tourId, form.Files, cloudinaryService, context);
                
                var response = new CreateTourWithImagesResponseDTO
                {
                    TourID = tourId,
                    Message = imageResponse.NewImageCount > 0 
                        ? $"Tạo tour thành công với {imageResponse.NewImageCount} ảnh"
                        : "Tạo tour thành công (không có ảnh)",
                    UploadedImageUrls = imageResponse.NewImageUrls,
                    FailedUploads = imageResponse.FailedUploads,
                    SuccessImageCount = imageResponse.NewImageCount,
                    FailedImageCount = imageResponse.FailedImageCount
                };

                return Results.Created($"/tours/{tourId}", response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating tour with images: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi tạo tour mới: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("CreateTourWithImages")
        .WithSummary("Tạo tour mới với ảnh")
        .WithDescription("Tạo tour mới và upload ảnh cùng lúc. Chỉ Admin và Supervisor mới được tạo tour. Sử dụng multipart/form-data với fields: tourName, address, province, price, maxPeople, description và files")
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
                                ["tourName"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tên tour"
                                },
                                ["description"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Mô tả tour"
                                },
                                ["address"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Địa chỉ tour"
                                },
                                ["province"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tỉnh/Thành phố"
                                },
                                ["price"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "number",
                                    Format = "double",
                                    Description = "Giá tour"
                                },
                                ["maxPeople"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "integer",
                                    Format = "int32",
                                    Description = "Số người tối đa (1-100)"
                                },
                                ["files"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "array",
                                    Items = new Microsoft.OpenApi.Models.OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary"
                                    },
                                    Description = "Danh sách ảnh tour (JPEG, PNG, GIF, WebP, max 5MB/file)"
                                }
                            },
                            Required = new HashSet<string> { "tourName", "address", "province", "price", "maxPeople" }
                        }
                    }
                }
            }
        })
        .Produces<CreateTourWithImagesResponseDTO>(201)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .RequireAuthorization("SupervisorOrAdmin");

        // 2. Lấy danh sách tour có phân trang
        tourGroup.MapGet("/", async ([FromServices] ITourService tourService, int page = 1, int size = 10) =>
        {
            try
            {
                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                var paginatedTours = await tourService.GetToursAsync(page, size);
                
                return Results.Ok(paginatedTours);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy danh sách tour: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetTours")
        .WithSummary("Lấy danh sách tour có phân trang")
        .WithDescription("Lấy danh sách tour với phân trang")
        .Produces<PaginatedToursResponseDTO>(200);

        // 3. Cập nhật thông tin tour với ảnh (đưa tourId lên param)
        tourGroup.MapPut("/{tourId:guid}", async (Guid tourId, HttpContext context, [FromServices] ITourService tourService, [FromServices] ICloudinaryService cloudinaryService) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Admin hoặc Supervisor
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được quản lý tour." }, statusCode: 403);
                }

                // Khởi tạo UpdateTourDTO
                var updateDto = new UpdateTourDTO();

                // Xử lý multipart form data
                if (context.Request.HasFormContentType)
                {
                    try
                    {
                var form = await context.Request.ReadFormAsync();
                
                        // Lấy thông tin text từ form
                        if (form.ContainsKey("tourName") && !string.IsNullOrWhiteSpace(form["tourName"].FirstOrDefault()))
                        {
                            var tourNameValue = form["tourName"].FirstOrDefault();
                            // Validate tourName với regex (hỗ trợ tiếng Việt có dấu)
                            var tourNameRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀ-ỹ0-9\s,./\-–—()&]{1,255}$");
                            if (!tourNameRegex.IsMatch(tourNameValue))
                                return Results.BadRequest(new { message = "Tên tour chứa ký tự không hợp lệ" });
                            
                            updateDto.TourName = tourNameValue;
                        }
                        
                        if (form.ContainsKey("description") && !string.IsNullOrWhiteSpace(form["description"].FirstOrDefault()))
                        {
                            var descriptionValue = form["description"].FirstOrDefault();
                            // Validate description với regex (hỗ trợ tiếng Việt có dấu)
                            var descriptionRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀ-ỹ0-9\s,./\-–—()&]{1,5000}$");
                            if (!descriptionRegex.IsMatch(descriptionValue))
                                return Results.BadRequest(new { message = "Mô tả chứa ký tự không hợp lệ" });
                            
                            updateDto.Description = descriptionValue;
                        }
                        
                        if (form.ContainsKey("address") && !string.IsNullOrWhiteSpace(form["address"].FirstOrDefault()))
                        {
                            var addressValue = form["address"].FirstOrDefault();
                            // Validate address với regex (hỗ trợ tiếng Việt có dấu)
                            var addressRegex = new System.Text.RegularExpressions.Regex(@"^[a-zA-ZÀ-ỹ0-9\s,./\-–—()&]{1,255}$");
                            if (!addressRegex.IsMatch(addressValue))
                                return Results.BadRequest(new { message = "Địa chỉ chứa ký tự không hợp lệ" });
                            
                            updateDto.Address = addressValue;
                        }
                        
                        if (form.ContainsKey("province") && !string.IsNullOrWhiteSpace(form["province"].FirstOrDefault()))
                        {
                            var provinceValue = form["province"].FirstOrDefault().Trim();
                            // TODO: Tạm thời comment check tỉnh để test API
                            // Validate province
                            // if (!ProvinceConstants.IsValidProvince(provinceValue))
                            //     return Results.BadRequest(new { message = "Tỉnh không hợp lệ" });
                            
                            updateDto.Province = provinceValue;
                        }

                        if (form.ContainsKey("price") && decimal.TryParse(form["price"].FirstOrDefault(), out var price))
                        {
                            if (price > 0)
                                updateDto.Price = price;
                            else
                                return Results.BadRequest(new { message = "Giá tour phải lớn hơn 0" });
                        }

                        if (form.ContainsKey("maxPeople") && int.TryParse(form["maxPeople"].FirstOrDefault(), out var maxPeople))
                        {
                            if (maxPeople >= 1 && maxPeople <= 100)
                                updateDto.MaxPeople = maxPeople;
                            else
                                return Results.BadRequest(new { message = "Số lượng người phải từ 1 đến 100" });
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

                        // Kiểm tra ảnh trước khi cập nhật tour
                        var imageValidationResult = ValidateTourImages(form.Files);
                        if (!imageValidationResult.IsValid)
                        {
                            return Results.BadRequest(new { 
                                message = "Có ảnh không hợp lệ", 
                                invalidFiles = imageValidationResult.InvalidFiles 
                            });
                        }

                        // Cập nhật thông tin tour
                        var success = await tourService.UpdateTourAsync(tourId, userId, updateDto);
                        if (!success)
                        {
                            return Results.NotFound(new { message = "Không tìm thấy tour hoặc bạn không có quyền cập nhật" });
                        }

                        // Xử lý ảnh theo logic mới
                        var imageUpdateDto = new TourImageUpdateDTO
                        {
                            TourId = tourId,
                            TourName = updateDto.TourName,
                            Description = updateDto.Description,
                            Address = updateDto.Address,
                            Province = updateDto.Province,
                            Price = updateDto.Price,
                            MaxPeople = updateDto.MaxPeople,
                            ExistingImageIds = existingImageIds.Any() ? existingImageIds : null,
                            DeleteImageIds = deleteImageIds.Any() ? deleteImageIds : null
                        };

                        var imageResponse = await ProcessTourImagesAsync(tourId, imageUpdateDto, form.Files, cloudinaryService, context);

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
                            var updateTourDto = System.Text.Json.JsonSerializer.Deserialize<UpdateTourDTO>(jsonString);
                            if (updateTourDto == null)
                                return Results.BadRequest(new { message = "Dữ liệu JSON không hợp lệ" });
                            
                            updateDto = updateTourDto;
                            
                            // Cập nhật thông tin tour
                            var success = await tourService.UpdateTourAsync(tourId, userId, updateDto);
                            
                            return success 
                                ? Results.Ok(new { message = "Cập nhật thông tin tour thành công" })
                                : Results.NotFound(new { message = "Không tìm thấy tour hoặc bạn không có quyền cập nhật" });
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
                Console.WriteLine($"Error updating tour with images: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật tour: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateTourWithImages")
        .WithSummary("Cập nhật tour với ảnh")
        .WithDescription("Cập nhật thông tin tour và upload ảnh cùng lúc. Chỉ Admin và Supervisor mới được cập nhật tour. Sử dụng multipart/form-data với fields: tourName, address, province, price, maxPeople, description, existingImageIds, deleteImageIds và files. Hoặc application/json với các trường cần cập nhật.")
        .WithOpenApi(operation => new Microsoft.OpenApi.Models.OpenApiOperation(operation)
        {
            Parameters = new List<Microsoft.OpenApi.Models.OpenApiParameter>
            {
                new Microsoft.OpenApi.Models.OpenApiParameter
                {
                    Name = "tourId",
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
                                ["tourName"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tên tour"
                                },
                                ["description"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Mô tả tour"
                                },
                                ["address"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Địa chỉ tour"
                                },
                                ["province"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tỉnh/Thành phố"
                                },
                                ["price"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "number",
                                    Format = "decimal",
                                    Description = "Giá tour"
                                },
                                ["maxPeople"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "integer",
                                    Format = "int32",
                                    Description = "Số người tối đa (1-100)"
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
                                    Description = "Danh sách ảnh tour mới (JPEG, PNG, GIF, WebP, max 5MB/file)"
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
                                ["tourName"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tên tour"
                                },
                                ["description"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Mô tả tour"
                                },
                                ["address"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Địa chỉ tour"
                                },
                                ["province"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Tỉnh/Thành phố"
                                },
                                ["price"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "number",
                                    Format = "decimal",
                                    Description = "Giá tour"
                                },
                                ["maxPeople"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "integer",
                                    Format = "int32",
                                    Description = "Số người tối đa (1-100)"
                                }
                            }
                        }
                    }
                }
            }
        })
        .Produces<TourImageUpdateResponseDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("SupervisorOrAdmin");

        // 4. Tìm kiếm tour theo keyword
        tourGroup.MapGet("/search", async ([FromServices] ITourService tourService, string? keyword = null, int page = 1, int size = 10) =>
        {
            try
            {
                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                var searchDto = new TourSearchDTO
                {
                    Keyword = keyword,
                    Page = page,
                    Size = size
                };

                var result = await tourService.SearchToursAsync(searchDto);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi tìm kiếm tour: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("SearchTours")
        .WithSummary("Tìm kiếm tour theo keyword")
        .WithDescription("Tìm kiếm tour theo tên tour (TourName)")
        .Produces<TourSearchResponseDTO>(200);

        // 4.5. Lấy danh sách tỉnh/thành phố
        tourGroup.MapGet("/provinces", async (ITourService tourService) =>
        {
            try
            {
                var provinces = await tourService.GetTourProvincesAsync();
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
        .WithName("GetTourProvinces")
        .WithSummary("Lấy danh sách tỉnh/thành phố")
        .WithDescription("Lấy danh sách tất cả các tỉnh/thành phố từ bảng Tours")
        .Produces<object>(200)
        .Produces(500)
        .AllowAnonymous();



        // 5. Lọc tour theo sao
        tourGroup.MapGet("/star/{star}", async (int star, [FromServices] ITourService tourService, int page = 1, int size = 10) =>
        {
            try
            {
                if (star < 1 || star > 5)
                    return Results.BadRequest(new { message = "Số sao phải từ 1 đến 5" });

                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                var searchDto = new TourSearchDTO
                {
                    Star = star,
                    Page = page,
                    Size = size
                };

                var result = await tourService.SearchToursAsync(searchDto);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lọc tour theo sao: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetToursByStar")
        .WithSummary("Lọc tour theo sao")
        .WithDescription("Lấy danh sách tour theo số sao")
        .Produces<TourSearchResponseDTO>(200)
        .Produces(400);

        // 6. Lọc tour theo tỉnh thành
        tourGroup.MapGet("/province/{province}", async (string province, [FromServices] ITourService tourService, int page = 1, int size = 10) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(province))
                    return Results.BadRequest(new { message = "Tỉnh/Thành phố không được để trống" });

                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                var searchDto = new TourSearchDTO
                {
                    Province = province,
                    Page = page,
                    Size = size
                };

                var result = await tourService.SearchToursAsync(searchDto);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lọc tour theo tỉnh thành: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetToursByProvince")
        .WithSummary("Lọc tour theo tỉnh thành")
        .WithDescription("Lấy danh sách tour theo tỉnh/thành phố")
        .Produces<TourSearchResponseDTO>(200)
        .Produces(400);

        // 6.5. Admin/Supervisor tìm kiếm tour theo status với keyword
        tourGroup.MapGet("/admin/search", async ([FromServices] ITourService tourService, HttpContext context, string? keyword = null, string? status = null, int page = 1, int size = 10) =>
        {
            try
            {
                // Kiểm tra quyền Admin hoặc Supervisor
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được tìm kiếm tour theo status." }, statusCode: 403);
                }

                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                // Parse status (nếu có)
                TourStatus? tourStatus = null;
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse<TourStatus>(status, true, out var parsedStatus))
                    {
                        tourStatus = parsedStatus;
                    }
                    else
                    {
                        return Results.BadRequest(new { message = "Status không hợp lệ. Các giá trị hợp lệ: Active, Suspend, Removed" });
                    }
                }

                var searchDto = new AdminTourSearchDTO
                {
                    Keyword = keyword,
                    Status = tourStatus,
                    Page = page,
                    Size = size
                };

                var result = await tourService.SearchToursForAdminAsync(searchDto);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi tìm kiếm tour: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("AdminSearchTours")
        .WithSummary("Admin/Supervisor tìm kiếm tour theo status với keyword")
        .WithDescription("Admin và Supervisor có thể tìm kiếm tour theo tên (keyword) và lọc theo status. Nếu không truyền status, sẽ lấy tất cả tour trừ Removed (Active + Suspend).")
        .Produces<TourSearchResponseDTO>(200)
        .Produces(400)
        .Produces(403)
        .RequireAuthorization("SupervisorOrAdmin");

        // 7. Soft delete tour
        tourGroup.MapPut("/delete/{tourId:guid}", async (Guid tourId, [FromServices] ITourService tourService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra quyền Admin hoặc Supervisor
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được quản lý tour." }, statusCode: 403);
                }

                var success = await tourService.SoftDeleteTourAsync(tourId, userId);
                
                return success 
                    ? Results.Ok(new { message = "Xóa tour thành công" })
                    : Results.NotFound(new { message = "Không tìm thấy tour hoặc bạn không có quyền xóa" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi xóa tour: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("SoftDeleteTour")
        .WithSummary("Soft delete tour")
        .WithDescription("Xóa tour (chuyển trạng thái thành Removed). Chỉ Admin và Supervisor mới được xóa tour.")
        .Produces(200)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("SupervisorOrAdmin");

        // 8a. Xem chi tiết tour - không có tourId
        tourGroup.MapGet("/detail", () =>
        {
            return Results.BadRequest(new { message = "tourId ko được để trống" });
        })
        .WithName("GetTourByIdEmpty")
        .WithSummary("Xem chi tiết tour - validation empty")
        .WithDescription("Trả về lỗi khi không truyền tourId")
        .Produces(400)
        .AllowAnonymous();

        // 8b. Xem chi tiết tour
        tourGroup.MapGet("/detail/{tourId}", async (string tourId, [FromServices] ITourService tourService) =>
        {
            try
            {
                // Kiểm tra empty
                if (string.IsNullOrWhiteSpace(tourId))
                {
                    return Results.BadRequest(new { message = "tourId ko được để trống" });
                }

                // Kiểm tra format (parse Guid)
                if (!Guid.TryParse(tourId, out var tourGuid))
                {
                    return Results.BadRequest(new { message = "tourID không hợp lệ" });
                }

                // Kiểm tra tourId có tồn tại không
                var tour = await tourService.GetTourByIdAsync(tourGuid);
                
                return tour != null 
                    ? Results.Ok(tour)
                    : Results.NotFound(new { message = "Không tìm thấy Tour" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy chi tiết tour: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetTourById")
        .WithSummary("Xem chi tiết tour")
        .WithDescription("Lấy thông tin chi tiết của một tour")
        .Produces<TourResponseDTO>(200)
        .Produces(400)
        .Produces(404)
        .AllowAnonymous(); // Public endpoint - không cần authentication

        // 9. Lấy danh sách tour theo status (ADMIN - SUPERVISOR)
        tourGroup.MapGet("/{status}", async (TourStatus status, [FromServices] ITourService tourService, HttpContext context, int page = 1, int size = 10) =>
        {
            try
            {
                // Kiểm tra quyền Admin hoặc Supervisor
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được xem danh sách tour theo status." }, statusCode: 403);
                }

                if (page < 1) page = 1;
                if (size < 1 || size > 100) size = 10;

                var paginatedTours = await tourService.GetToursByStatusAsync(status, page, size);
                
                return Results.Ok(paginatedTours);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy danh sách tour theo status: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetToursByStatus")
        .WithSummary("Lấy danh sách tour theo status")
        .WithDescription("Admin và Supervisor có thể lấy danh sách tour theo trạng thái (Active, Suspend, Removed) với phân trang")
        .Produces<PaginatedToursResponseDTO>(200)
        .Produces(403)
        .RequireAuthorization("SupervisorOrAdmin");

        // 10. Cập nhật status tour (ADMIN - SUPERVISOR)
        tourGroup.MapPut("/{tourId:guid}/status", async (Guid tourId, [FromBody] UpdateTourStatusDTO updateStatusDto, [FromServices] ITourService tourService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Admin hoặc Supervisor
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được cập nhật status tour." }, statusCode: 403);
                }

                var success = await tourService.UpdateTourStatusAsync(tourId, updateStatusDto.Status);
                
                if (!success)
                {
                    return Results.NotFound(new { message = "Không tìm thấy tour hoặc status không hợp lệ" });
                }

                return Results.Ok(new { message = "Cập nhật status tour thành công", status = updateStatusDto.Status });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật status tour: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateTourStatus")
        .WithSummary("Cập nhật status tour")
        .WithDescription("Admin và Supervisor có thể cập nhật trạng thái tour (Active, Suspend, Removed)")
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

    private static ImageValidationResult ValidateTourImages(IFormFileCollection files)
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

    // Helper method để xử lý ảnh mới cho tour (POST endpoint)
    private static async Task<TourImageUpdateResponseDTO> ProcessNewTourImagesAsync(
        Guid tourId, 
        IFormFileCollection files, 
        ICloudinaryService cloudinaryService, 
        HttpContext context)
    {
        var response = new TourImageUpdateResponseDTO();
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
                        var imageUrl = await cloudinaryService.UploadImageAsync(file, "tours");
                        
                        // Lưu vào database
                        var image = new Image
                        {
                            TableType = TableTypeImage.Tour,
                            TypeID = tourId,
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
            Console.WriteLine($"Error in ProcessNewTourImagesAsync: {ex.Message}");
            response.Message = $"Có lỗi xảy ra khi upload ảnh: {ex.Message}";
        }

        return response;
    }

    // Helper method để xử lý ảnh theo logic mới cho tour (PUT endpoint)
    private static async Task<TourImageUpdateResponseDTO> ProcessTourImagesAsync(
        Guid tourId, 
        TourImageUpdateDTO imageUpdateDto, 
        IFormFileCollection files, 
        ICloudinaryService cloudinaryService, 
        HttpContext context)
    {
        var response = new TourImageUpdateResponseDTO();
        var existingImageUrls = new List<string>();
        var newImageUrls = new List<string>();
        var deletedImageUrls = new List<string>();
        var failedUploads = new List<string>();

        using var scope = context.RequestServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // 1. Lấy danh sách ảnh hiện tại của tour
            var currentImages = await dbContext.Images
                .Where(img => img.TableType == TableTypeImage.Tour && img.TypeID == tourId)
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
                        var imageUrl = await cloudinaryService.UploadImageAsync(file, "tours");
                        
                        // Lưu vào database
                        var image = new Image
                        {
                            TableType = TableTypeImage.Tour,
                            TypeID = tourId,
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
                ? $"Cập nhật ảnh tour thành công. {string.Join(", ", messageParts)}."
                : "Cập nhật ảnh tour thành công (không có thay đổi).";

            if (response.FailedImageCount > 0)
            {
                response.Message += $" {response.FailedImageCount} ảnh xử lý thất bại.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ProcessTourImagesAsync: {ex.Message}");
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
