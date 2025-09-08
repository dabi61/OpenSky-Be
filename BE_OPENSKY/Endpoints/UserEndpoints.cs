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
        var userGroup = app.MapGroup("/api/users")
            .WithTags("User Management")
            .WithOpenApi();

        // Admin có thể tạo tài khoản Supervisor
        userGroup.MapPost("/create-supervisor", async ([FromBody] CreateUserDTO createUserDto, [FromServices] IUserService userService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Admin
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

                // Tạo tài khoản Supervisor
                var registerDto = new UserRegisterDTO
                {
                    Email = createUserDto.Email,
                    Password = createUserDto.Password,
                    FullName = createUserDto.FullName
                };

                var user = await userService.CreateWithRoleAsync(registerDto, RoleConstants.Supervisor);
                return Results.Created($"/api/users/{user.UserID}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi tạo tài khoản Supervisor",
                    statusCode: 500
                );
            }
        })
        .WithName("CreateSupervisor")
        .WithSummary("Admin tạo tài khoản Supervisor")
        .WithDescription("Chỉ Admin có thể tạo tài khoản cho Supervisor")
        .Produces<UserResponseDTO>(201)
        .Produces(400)
        .Produces(403)
        .RequireAuthorization("AdminOnly");

        // Supervisor có thể tạo tài khoản TourGuide
        userGroup.MapPost("/create-tourguide", async ([FromBody] CreateUserDTO createUserDto, [FromServices] IUserService userService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra quyền Supervisor hoặc Admin
                if (!context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.Admin))
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

                // Tạo tài khoản TourGuide
                var registerDto = new UserRegisterDTO
                {
                    Email = createUserDto.Email,
                    Password = createUserDto.Password,
                    FullName = createUserDto.FullName
                };

                var user = await userService.CreateWithRoleAsync(registerDto, RoleConstants.TourGuide);
                return Results.Created($"/api/users/{user.UserID}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi tạo tài khoản Tour Guide",
                    statusCode: 500
                );
            }
        })
        .WithName("CreateTourGuide")
        .WithSummary("Supervisor tạo tài khoản Tour Guide")
        .WithDescription("Supervisor hoặc Admin có thể tạo tài khoản cho Tour Guide")
        .Produces<UserResponseDTO>(201)
        .Produces(400)
        .Produces(403)
        .RequireAuthorization("SupervisorOrAdmin");

        // Customer đăng ký mở khách sạn (chuyển từ Customer -> Hotel sau khi được duyệt)
        userGroup.MapPost("/apply-hotel", async ([FromBody] HotelApplicationDTO applicationDto, [FromServices] IHotelService hotelService, HttpContext context) =>
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

        // Admin xem tất cả đơn đăng ký khách sạn chờ duyệt
        userGroup.MapGet("/pending-hotels", async ([FromServices] IHotelService hotelService, HttpContext context) =>
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

        // Admin xem chi tiết đơn đăng ký khách sạn
        userGroup.MapGet("/pending-hotels/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context) =>
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

        // Admin duyệt đơn đăng ký khách sạn
        userGroup.MapPost("/approve-hotel/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context) =>
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

        // Admin từ chối đơn đăng ký khách sạn
        userGroup.MapDelete("/reject-hotel/{hotelId:guid}", async (Guid hotelId, [FromServices] IHotelService hotelService, HttpContext context) =>
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

        // Customer xem đơn đăng ký khách sạn của mình
        userGroup.MapGet("/my-hotels", async ([FromServices] IHotelService hotelService, HttpContext context) =>
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

        // Lấy danh sách người dùng với phân trang (phân quyền theo role)
        userGroup.MapGet("/", async ([FromServices] IUserService userService, HttpContext context, int page = 1, int limit = 10, string? role = null) =>
        {
            try
            {
                // Chỉ Admin và Supervisor mới có quyền xem danh sách user
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (limit < 1 || limit > 100) limit = 10; // Giới hạn tối đa 100 items per page

                var result = await userService.GetUsersPaginatedAsync(page, limit, role);
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
                                updateDto.DoB = DateOnly.FromDateTime(doB);
                            }
                            // Nếu không được, thử parse với format mặc định
                            else if (DateTime.TryParse(doBString, out doB))
                            {
                                updateDto.DoB = DateOnly.FromDateTime(doB);
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

    }
}

