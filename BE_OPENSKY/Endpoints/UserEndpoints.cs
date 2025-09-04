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
        userGroup.MapPost("/create-supervisor", async (CreateUserDTO createUserDto, IUserService userService, HttpContext context) =>
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
                    FullName = createUserDto.FullName,
                    PhoneNumber = createUserDto.PhoneNumber
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
        userGroup.MapPost("/create-tourguide", async (CreateUserDTO createUserDto, IUserService userService, HttpContext context) =>
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
                    FullName = createUserDto.FullName,
                    PhoneNumber = createUserDto.PhoneNumber
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
        userGroup.MapPost("/apply-hotel", async (HotelApplicationDTO applicationDto, IHotelService hotelService, HttpContext context) =>
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
                
                if (string.IsNullOrWhiteSpace(applicationDto.District))
                    return Results.BadRequest(new { message = "Quận/Huyện không được để trống" });

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
        userGroup.MapGet("/pending-hotels", async (IHotelService hotelService, HttpContext context) =>
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
        userGroup.MapGet("/pending-hotels/{hotelId:guid}", async (Guid hotelId, IHotelService hotelService, HttpContext context) =>
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
        userGroup.MapPost("/approve-hotel/{hotelId:guid}", async (Guid hotelId, IHotelService hotelService, HttpContext context) =>
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
        userGroup.MapDelete("/reject-hotel/{hotelId:guid}", async (Guid hotelId, IHotelService hotelService, HttpContext context) =>
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
        userGroup.MapGet("/my-hotels", async (IHotelService hotelService, HttpContext context) =>
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

        // Lấy danh sách người dùng (phân quyền theo role)
        userGroup.MapGet("/", async (IUserService userService, HttpContext context, string? role = null) =>
        {
            try
            {
                // Chỉ Admin và Supervisor mới có quyền xem danh sách user
                if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                {
                    return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                }

                var users = await userService.GetUsersAsync(role);
                return Results.Ok(users);
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
        .WithSummary("Lấy danh sách người dùng")
        .WithDescription("Admin và Supervisor có thể xem danh sách người dùng")
        .Produces<List<UserResponseDTO>>(200)
        .Produces(403)
        .RequireAuthorization("SupervisorOrAdmin");

        // Xem thông tin cá nhân
        userGroup.MapGet("/profile", async (IUserService userService, HttpContext context) =>
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

        // Cập nhật thông tin cá nhân
        userGroup.MapPut("/profile", async (UpdateProfileDTO updateDto, IUserService userService, HttpContext context) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var updatedProfile = await userService.UpdateProfileAsync(userId, updateDto);
                return Results.Ok(updatedProfile);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi cập nhật thông tin cá nhân",
                    statusCode: 500
                );
            }
        })
        .WithName("UpdateProfile")
        .WithSummary("Cập nhật thông tin cá nhân")
        .WithDescription("User có thể cập nhật thông tin cá nhân của mình")
        .Produces<ProfileResponseDTO>(200)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization("AuthenticatedOnly");

        // Upload avatar - Smart endpoint (supports both multipart and raw binary)
        userGroup.MapPost("/profile/avatar", async (HttpContext context, IUserService userService, ICloudinaryService cloudinaryService) =>
        {
            try
            {
                // Lấy user ID từ JWT token
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                var contentType = context.Request.ContentType;
                IFormFile? file = null;

                // Kiểm tra xem là multipart hay raw binary
                if (context.Request.HasFormContentType)
                {
                    // Multipart form data
                    try
                    {
                        var form = await context.Request.ReadFormAsync();
                        file = form.Files.FirstOrDefault() ?? 
                               form.Files.GetFile("file") ?? 
                               form.Files.GetFile("avatar") ?? 
                               form.Files.GetFile("image");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Multipart parsing failed: {ex.Message}");
                        // Fallback to raw binary if multipart fails
                    }
                }

                // Nếu không có file từ multipart hoặc không phải multipart, thử raw binary
                if (file == null)
                {
                    if (IsImageContentType(contentType))
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
                                suggestion = "Sử dụng Postman với form-data (key: 'file') hoặc raw binary với Content-Type: image/jpeg"
                            });
                        }

                        if (fileBytes.Length > 5 * 1024 * 1024) // 5MB
                        {
                            return Results.BadRequest(new { message = "File không được vượt quá 5MB" });
                        }

                        var fileName = $"avatar_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
                        file = new FormFileFromBytes(fileBytes, fileName, contentType ?? "image/jpeg");
                    }
                    else
                    {
                        return Results.BadRequest(new { 
                            message = "Không tìm thấy file hoặc Content-Type không hợp lệ",
                            receivedContentType = contentType,
                            supportedFormats = new[] { "multipart/form-data", "image/jpeg", "image/png", "image/gif", "image/webp" }
                        });
                    }
                }

                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { message = "File không được để trống" });
                }
                
                Console.WriteLine($"Uploading file: {file.FileName}, Size: {file.Length}, Content-Type: {file.ContentType}");
                
                // Upload ảnh lên Cloudinary
                var avatarUrl = await cloudinaryService.UploadImageAsync(file, "avatars");
                
                // Cập nhật avatar URL vào database
                var updatedProfile = await userService.UpdateAvatarAsync(userId, avatarUrl);
                
                return Results.Ok(new { 
                    message = "Upload avatar thành công",
                    profile = updatedProfile 
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
                    detail: $"Có lỗi xảy ra khi upload avatar: {ex.Message}",
                    statusCode: 500
                );
            }
        })
        .WithName("UploadAvatar")
        .WithSummary("Upload ảnh đại diện")
        .WithDescription("Upload ảnh đại diện - hỗ trợ cả multipart/form-data và raw binary")
        .Accepts<IFormFile>("multipart/form-data")
        .Accepts<byte[]>("image/jpeg", "image/png", "image/gif")
        .Produces<ProfileResponseDTO>(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .RequireAuthorization("AuthenticatedOnly");

        // Quên mật khẩu
        userGroup.MapPost("/forgot-password", async (ForgotPasswordDTO forgotPasswordDto, IPasswordResetService passwordResetService, IEmailService emailService) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(forgotPasswordDto.Email))
                {
                    return Results.BadRequest(new { message = "Email không được để trống" });
                }

                var token = await passwordResetService.GenerateResetTokenAsync(forgotPasswordDto.Email);
                
                // Gửi email với token
                var emailSent = await emailService.SendPasswordResetEmailAsync(forgotPasswordDto.Email, token);
                
                if (!emailSent)
                {
                    return Results.Problem(
                        title: "Lỗi gửi email",
                        detail: "Không thể gửi email reset mật khẩu. Vui lòng thử lại sau.",
                        statusCode: 500
                    );
                }
                
                return Results.Ok(new { 
                    message = "Mã reset mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.",
                    // Trong development, có thể trả về token để test
                    // token = token // Chỉ dùng trong development
                });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi xử lý yêu cầu reset mật khẩu",
                    statusCode: 500
                );
            }
        })
        .WithName("ForgotPassword")
        .WithSummary("Quên mật khẩu")
        .WithDescription("Gửi mã reset mật khẩu đến email")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Reset mật khẩu
        userGroup.MapPost("/reset-password", async (ResetPasswordDTO resetPasswordDto, IPasswordResetService passwordResetService) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(resetPasswordDto.Token))
                {
                    return Results.BadRequest(new { message = "Token không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(resetPasswordDto.NewPassword))
                {
                    return Results.BadRequest(new { message = "Mật khẩu mới không được để trống" });
                }

                if (resetPasswordDto.NewPassword != resetPasswordDto.ConfirmPassword)
                {
                    return Results.BadRequest(new { message = "Mật khẩu xác nhận không khớp" });
                }

                var success = await passwordResetService.ResetPasswordAsync(resetPasswordDto.Token, resetPasswordDto.NewPassword);
                
                if (success)
                {
                    return Results.Ok(new { 
                        message = "Mật khẩu đã được reset thành công. Bạn có thể đăng nhập với mật khẩu mới."
                    });
                }
                else
                {
                    return Results.BadRequest(new { 
                        message = "Token không hợp lệ hoặc đã hết hạn. Vui lòng thử lại."
                    });
                }
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi reset mật khẩu",
                    statusCode: 500
                );
            }
        })
        .WithName("ResetPassword")
        .WithSummary("Reset mật khẩu")
        .WithDescription("Reset mật khẩu bằng token")
        .Produces(200)
        .Produces(400)
        .Produces(500);

        // Validate reset token
        userGroup.MapGet("/validate-reset-token/{token}", async (string token, IPasswordResetService passwordResetService) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Results.BadRequest(new { message = "Token không được để trống" });
                }

                // logger.LogInformation("Validating reset token: {Token}", token);
                
                var isValid = await passwordResetService.ValidateResetTokenAsync(token);
                
                return Results.Ok(new { 
                    isValid = isValid,
                    message = isValid ? "Token hợp lệ" : "Token không hợp lệ hoặc đã hết hạn"
                });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống",
                    detail: "Có lỗi xảy ra khi validate token",
                    statusCode: 500
                );
            }
        })
        .WithName("ValidateResetToken")
        .WithSummary("Kiểm tra tính hợp lệ của reset token")
        .WithDescription("Kiểm tra tính hợp lệ của reset token")
        .Produces(200)
        .Produces(400)
        .Produces(500);
    }

    private static bool IsImageContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;
            
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
