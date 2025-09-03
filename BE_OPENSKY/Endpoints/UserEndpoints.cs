namespace BE_OPENSKY.Endpoints;

public static class UserEndpoints
{
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
    }
}
