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

                if (!form.TryGetValue("address", out var addressValue) || string.IsNullOrWhiteSpace(addressValue))
                    return Results.BadRequest(new { message = "Địa chỉ không được để trống" });

                if (!form.TryGetValue("province", out var provinceValue) || string.IsNullOrWhiteSpace(provinceValue))
                    return Results.BadRequest(new { message = "Tỉnh/Thành phố không được để trống" });

                // Remove star validation
                // if (!int.TryParse(form["star"], out var star) || star < 1 || star > 5)
                //     return Results.BadRequest(new { message = "Số sao phải từ 1 đến 5" });

                if (!decimal.TryParse(form["price"], out var price) || price <= 0)
                    return Results.BadRequest(new { message = "Giá tour phải lớn hơn 0" });

                if (!int.TryParse(form["maxPeople"], out var maxPeople) || maxPeople < 1 || maxPeople > 100)
                    return Results.BadRequest(new { message = "Số người tối đa phải từ 1 đến 100" });

                // Tạo DTO cho tour
                var createTourDto = new CreateTourDTO
                {
                    TourName = tourNameValue.ToString().Trim(),
                    Description = form["description"].ToString(),
                    Address = addressValue.ToString().Trim(),
                    Province = provinceValue.ToString().Trim(),
                    Price = price,
                    MaxPeople = maxPeople
                };

                // Tạo tour mới
                var tourId = await tourService.CreateTourAsync(userId, createTourDto);

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
                            var imageUrl = await cloudinaryService.UploadImageAsync(file, "tours");
                            
                            // Lưu vào database
                            var image = new Image
                            {
                                TableType = TableTypeImage.Tour,
                                TypeID = tourId,
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
                
                var response = new CreateTourWithImagesResponseDTO
                {
                    TourID = tourId,
                    Message = uploadedImageUrls.Count > 0 
                        ? $"Tạo tour thành công với {uploadedImageUrls.Count} ảnh"
                        : "Tạo tour thành công (không có ảnh)",
                    UploadedImageUrls = uploadedImageUrls,
                    FailedUploads = failedUploads,
                    SuccessImageCount = uploadedImageUrls.Count,
                    FailedImageCount = failedUploads.Count
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

        // 3. Cập nhật thông tin tour với ảnh
        tourGroup.MapPut("/", async (HttpContext context, [FromServices] ITourService tourService, [FromServices] ICloudinaryService cloudinaryService) =>
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
                if (!form.TryGetValue("tourId", out var tourIdValue) || !Guid.TryParse(tourIdValue, out var tourId))
                    return Results.BadRequest(new { message = "Tour ID không hợp lệ" });

                // Tạo DTO cho tour
                var updateDto = new UpdateTourDTO
                {
                    TourName = form["tourName"].ToString(),
                    Description = form["description"].ToString(),
                    Address = form["address"].ToString(),
                    Province = form["province"].ToString(),
                    Price = decimal.TryParse(form["price"], out var price) ? price : null,
                    MaxPeople = int.TryParse(form["maxPeople"], out var maxPeople) ? maxPeople : null
                };

                // Cập nhật tour
                var success = await tourService.UpdateTourAsync(tourId, userId, updateDto);
                
                if (!success)
                {
                    return Results.NotFound(new { message = "Không tìm thấy tour hoặc bạn không có quyền cập nhật" });
                }

                // Xử lý upload ảnh nếu có
                var uploadedImageUrls = new List<string>();
                var failedUploads = new List<string>();
                var imageAction = form["imageAction"].ToString().ToLower();

                if (form.Files.Count > 0)
                {
                    // Xóa ảnh cũ nếu replace
                    if (imageAction == "replace")
                    {
                        using var scope = context.RequestServices.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var existingImages = await dbContext.Images
                            .Where(i => i.TableType == TableTypeImage.Tour && i.TypeID == tourId)
                            .ToListAsync();
                        
                        dbContext.Images.RemoveRange(existingImages);
                        await dbContext.SaveChangesAsync();
                    }

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
                            var imageUrl = await cloudinaryService.UploadImageAsync(file, "tours");
                            
                            // Lưu vào database
                            var image = new Image
                            {
                                TableType = TableTypeImage.Tour,
                                TypeID = tourId,
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
                
                var response = new UpdateTourWithImagesResponseDTO
                {
                    TourID = tourId,
                    Message = uploadedImageUrls.Count > 0 
                        ? $"Cập nhật tour thành công với {uploadedImageUrls.Count} ảnh"
                        : "Cập nhật tour thành công (không có ảnh)",
                    UploadedImageUrls = uploadedImageUrls,
                    FailedUploads = failedUploads,
                    SuccessImageCount = uploadedImageUrls.Count,
                    FailedImageCount = failedUploads.Count
                };

                return Results.Ok(response);
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
        .WithDescription("Cập nhật thông tin tour và upload ảnh cùng lúc. Chỉ Admin và Supervisor mới được cập nhật tour. Sử dụng multipart/form-data với fields: tourId, tourName, address, province, price, maxPeople, description, imageAction và files")
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
                                ["tourId"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "uuid",
                                    Description = "ID của tour cần cập nhật"
                                },
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
                                ["imageAction"] = new Microsoft.OpenApi.Models.OpenApiSchema
                                {
                                    Type = "string",
                                    Description = "Hành động ảnh: add (thêm), replace (thay thế), remove (xóa)"
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
                            Required = new HashSet<string> { "tourId" }
                        }
                    }
                }
            }
        })
        .Produces<UpdateTourWithImagesResponseDTO>(200)
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
        .WithDescription("Tìm kiếm tour theo tên")
        .Produces<TourSearchResponseDTO>(200);

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

        // 8. Xem chi tiết tour
        tourGroup.MapGet("/{tourId:guid}", async (Guid tourId, [FromServices] ITourService tourService) =>
        {
            try
            {
                var tour = await tourService.GetTourByIdAsync(tourId);
                
                return tour != null 
                    ? Results.Ok(tour)
                    : Results.NotFound(new { message = "Không tìm thấy tour" });
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
        .Produces(404);
    }

    private static bool IsImageContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;
            
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
