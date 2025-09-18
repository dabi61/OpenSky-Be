namespace BE_OPENSKY.Endpoints;

public static class UserEndpoints
{
    private static string? GetBoundaryFromContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return null;
            
        var boundaryIndex = contentType.IndexOf("boundary=");
        if (boundaryIndex == -1)
            return null;
            
        return contentType.Substring(boundaryIndex + "boundary=".Length);
    }

    public static void MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup("/users")
            .WithTags("User")
            .WithOpenApi();

        // Lấy danh sách người dùng với phân trang (phân quyền theo role)
        userGroup.MapGet("/", async (
        [FromServices] IUserService userService,
        HttpContext context,
        int page = 1,
        int limit = 10,
        [FromQuery] string[]? roles = null) =>
        {
            try
            {
                // Chỉ Admin và Supervisor mới có quyền xem danh sách user
                if (!context.User.IsInRole(RoleConstants.Admin) &&
                    !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                // Convert array sang List để truyền xuống service
                var rolesList = roles?.ToList();

                var result = await userService.GetUsersPaginatedAsync(page, limit, rolesList);
                return Results.Ok(result);
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi lấy danh sách người dùng",
                    statusCode: 500
                );
            }
        })
        .WithName("GetUsers")
        .WithSummary("Lấy danh sách người dùng với phân trang")
        .WithDescription("Admin và Supervisor có thể xem danh sách người dùng với phân trang. Hỗ trợ lọc theo role.")
        .Produces<PaginatedUsersResponseDTO>(200)
        .Produces(403)
        .RequireAuthorization("SupervisorOrAdmin");


        // Lấy danh sách người dùng với phân trang, lọc role và tìm kiếm
        userGroup.MapGet("/search", async (
        [FromServices] IUserService userService,
        HttpContext context,
        int page = 1,
        int limit = 10,
        [FromQuery] string[]? roles = null,  
        [FromQuery] string? keyword = null) =>
        {
            try
            {
                if (!context.User.IsInRole(RoleConstants.Admin) &&
                    !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10;

                var rolesList = roles?.ToList(); 
                var result = await userService.SearchUsersPaginatedAsync(page, limit, rolesList, keyword);

                return Results.Ok(result);
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi lấy danh sách người dùng",
                    statusCode: 500
                );
            }
        })
        .WithName("SearchUsers")
        .WithSummary("Lấy danh sách người dùng với phân trang, lọc role và tìm kiếm")
        .WithDescription("Admin và Supervisor có thể xem danh sách người dùng với phân trang. Hỗ trợ lọc theo nhiều role và tìm kiếm theo từ khóa.")
        .Produces<PaginatedUsersResponseDTO>(200)
        .Produces(403)
        .RequireAuthorization("SupervisorOrAdmin");

        // Xem thông tin cá nhân
        userGroup.MapGet("/profile", async ([FromServices] IUserService userService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var profile = await userService.GetProfileAsync(userId);
                return profile != null 
                    ? Results.Ok(profile)
                    : Results.NotFound(new { message = "Không tìm thấy thông tin người dùng" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi lấy thông tin cá nhân",
                    statusCode: 500
                );
            }
        })
        .WithName("GetProfile")
        .WithSummary("Xem thông tin cá nhân")
        .WithDescription("User có thể xem thông tin cá nhân của mình")
        .Produces<ProfileResponseDTO>(200)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization("AuthenticatedOnly");

        // Cập nhật thông tin cá nhân và upload avatar (gộp 2 API thành 1)
        userGroup.MapPut("/profile", async (HttpContext context, [FromServices] IUserService userService, [FromServices] ICloudinaryService cloudinaryService) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Khởi tạo UpdateProfileDTO
                var updateDto = new UpdateProfileDTO();

                // Xử lý multipart form data
                if (context.Request.HasFormContentType)
                {
                    try
                    {
                        var form = await context.Request.ReadFormAsync();
                        
                        // Lấy thông tin text từ form
                        if (form.ContainsKey("fullName"))
                            updateDto.FullName = form["fullName"].FirstOrDefault();
                        if (form.ContainsKey("phoneNumber"))
                            updateDto.PhoneNumber = form["phoneNumber"].FirstOrDefault();
                        if (form.ContainsKey("citizenId"))
                            updateDto.CitizenId = form["citizenId"].FirstOrDefault();
                        if (form.ContainsKey("doB") && !string.IsNullOrEmpty(form["doB"].FirstOrDefault()))
                        {
                            var doBString = form["doB"].FirstOrDefault();
                            // Thử parse với format dd-MM-yyyy trước
                            if (DateTime.TryParseExact(doBString, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var doB))
                            {
                                updateDto.dob = DateOnly.FromDateTime(doB);
                            }
                            // Nếu không được, thử parse với format mặc định
                            else if (DateTime.TryParse(doBString, out doB))
                            {
                                updateDto.dob = DateOnly.FromDateTime(doB);
                            }
                        }

                        // Lấy file avatar từ form
                        var avatarFile = form.Files.FirstOrDefault() ?? 
                                       form.Files.GetFile("file") ?? 
                                       form.Files.GetFile("avatar") ?? 
                                       form.Files.GetFile("image");

                        if (avatarFile != null && avatarFile.Length > 0)
                        {
                            Console.WriteLine($"Uploading avatar: {avatarFile.FileName}, Size: {avatarFile.Length}, Content-Type: {avatarFile.ContentType}");
                            
                            // Upload ảnh lên Cloudinary
                            var avatarUrl = await cloudinaryService.UploadImageAsync(avatarFile, "avatars");
                            
                            // Cập nhật avatar URL vào database
                            await userService.UpdateAvatarAsync(userId, avatarUrl);
                        }
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
                            updateDto = System.Text.Json.JsonSerializer.Deserialize<UpdateProfileDTO>(jsonString) ?? new UpdateProfileDTO();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"JSON parsing failed: {ex.Message}");
                        return Results.BadRequest(new { message = "Lỗi khi xử lý dữ liệu JSON" });
                    }
                }

                // Cập nhật thông tin profile
                var updatedProfile = await userService.UpdateProfileAsync(userId, updateDto);
                
                return Results.Ok(new { 
                    message = "Cập nhật thông tin thành công",
                    profile = updatedProfile 
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật thông tin: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateProfile")
        .WithSummary("Cập nhật thông tin cá nhân và upload avatar")
        .WithDescription("Cập nhật thông tin cá nhân và upload avatar trong một request. Hỗ trợ multipart/form-data với các trường text và file")
        .Accepts<UpdateProfileWithAvatarDTO>("multipart/form-data")
        .Produces<ProfileResponseDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization("AuthenticatedOnly");

        // Admin endpoints
        // 1. Admin xem thông tin user theo ID
        userGroup.MapGet("/{userId:guid}", async (Guid userId, [FromServices] IUserService userService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Admin
                if (!context.User.IsInRole(RoleConstants.Admin))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                var user = await userService.GetUserByIdAsync(userId);
                return user != null 
                    ? Results.Ok(user)
                    : Results.NotFound(new { message = "Không tìm thấy người dùng" });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi lấy thông tin người dùng: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("GetUserById")
        .WithSummary("Admin xem thông tin user theo ID")
        .WithDescription("Admin có thể xem thông tin chi tiết của bất kỳ user nào theo ID")
        .Produces<UserResponseDTO>(200)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("AdminOnly");


        // 1b. Admin tạo người dùng với avatar (multipart/form-data)
        userGroup.MapPost("/", async (
            [FromForm] AdminCreateUserWithAvatarDTO createUserDto,
            [FromServices] IUserService userService, 
            [FromServices] ICloudinaryService cloudinaryService,
            HttpContext context) =>
        {
            try
            {
                // Chỉ Admin mới có quyền tạo user
                if (!context.User.IsInRole(RoleConstants.Admin))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(createUserDto.Email))
                    return Results.BadRequest(new { message = "Email không được để trống" });
                
                if (string.IsNullOrWhiteSpace(createUserDto.Password))
                    return Results.BadRequest(new { message = "Mật khẩu không được để trống" });
                
                if (string.IsNullOrWhiteSpace(createUserDto.FullName))
                    return Results.BadRequest(new { message = "Họ tên không được để trống" });

                if (string.IsNullOrWhiteSpace(createUserDto.Role))
                    return Results.BadRequest(new { message = "Role không được để trống" });

                // Admin có thể tạo tất cả role
                var allowedRoles = new[] { RoleConstants.Admin, RoleConstants.Supervisor, RoleConstants.TourGuide, RoleConstants.Customer, RoleConstants.Hotel };

                // Kiểm tra role hợp lệ
                if (!allowedRoles.Contains(createUserDto.Role))
                {
                    return Results.BadRequest(new { message = $"Role không hợp lệ: {createUserDto.Role}" });
                }

                // Parse DateOnly nếu có
                DateOnly? dob = null;
                if (!string.IsNullOrWhiteSpace(createUserDto.dob) && DateOnly.TryParse(createUserDto.dob, out var parsedDob))
                {
                    dob = parsedDob;
                }

                // Tạo DTO cho service
                var adminCreateUserDto = new AdminCreateUserDTO
                {
                    Email = createUserDto.Email,
                    Password = createUserDto.Password,
                    FullName = createUserDto.FullName,
                    Role = createUserDto.Role,
                    PhoneNumber = createUserDto.PhoneNumber,
                    CitizenId = createUserDto.CitizenId,
                    dob = dob
                };

                // Upload avatar nếu có
                string? avatarUrl = null;
                if (createUserDto.Avatar != null && createUserDto.Avatar.Length > 0)
                {
                    try
                    {
                        avatarUrl = await cloudinaryService.UploadImageAsync(createUserDto.Avatar, "avatars");
                    }
                    catch (Exception ex)
                    {
                        return Results.BadRequest(new { message = $"Lỗi khi upload avatar: {ex.Message}" });
                    }
                }

                // Tạo user với avatar
                var user = await userService.CreateAdminUserWithAvatarAsync(adminCreateUserDto, avatarUrl);
                return Results.Created($"/users/{user.UserID}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi tạo người dùng: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("CreateUserWithAvatar")
        .WithSummary("Admin tạo người dùng với avatar")
        .WithDescription("Admin có thể tạo người dùng với avatar (multipart/form-data). Hỗ trợ upload ảnh đại diện.")
        .WithOpenApi()
        .Produces<UserResponseDTO>(201)
        .Produces(400)
        .Produces(403)
        .DisableAntiforgery()
        .RequireAuthorization("AdminOnly");


        // 2. Admin quản lý status người dùng
        userGroup.MapPut("/{userId:guid}/status", async (Guid userId, [FromBody] UpdateUserStatusDTO updateStatusDto, [FromServices] IUserService userService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Admin
                if (!context.User.IsInRole(RoleConstants.Admin))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Validate input
                if (!Enum.IsDefined(typeof(UserStatus), updateStatusDto.Status))
                    return Results.BadRequest(new { message = "Status không hợp lệ" });

                // Lấy Admin ID từ JWT token
                var adminIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Không cho phép Admin tự thay đổi status của mình
                if (userId == adminId)
                {
                    return Results.BadRequest(new { message = "Bạn không thể thay đổi trạng thái của chính mình" });
                }

                var success = await userService.UpdateUserStatusAsync(userId, updateStatusDto.Status, adminId);
                
                return success 
                    ? Results.Ok(new { message = "Cập nhật trạng thái người dùng thành công" })
                    : Results.NotFound(new { message = "Không tìm thấy người dùng" });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: $"Có lỗi xảy ra khi cập nhật trạng thái người dùng: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateUserStatus")
        .WithSummary("Admin quản lý status người dùng")
        .WithDescription("Admin có thể thay đổi trạng thái người dùng (Active, Banned)")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .Produces(403)
        .Produces(404)
        .RequireAuthorization("AdminOnly");

    }
}

