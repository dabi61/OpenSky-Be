namespace BE_OPENSKY.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        // Đăng ký người dùng
        authGroup.MapPost("/register", async (UserRegisterDTO userDto, IUserService userService) =>
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(userDto.Email))
                    return Results.BadRequest(new { message = "Email không được để trống" });
                
                if (string.IsNullOrWhiteSpace(userDto.Password))
                    return Results.BadRequest(new { message = "Mật khẩu không được để trống" });
                
                if (string.IsNullOrWhiteSpace(userDto.FullName))
                    return Results.BadRequest(new { message = "Họ tên không được để trống" });
                
                if (userDto.Password.Length < 6)
                    return Results.BadRequest(new { message = "Mật khẩu phải có ít nhất 6 ký tự" });

                if (!IsValidEmail(userDto.Email))
                    return Results.BadRequest(new { message = "Email không hợp lệ" });

                var user = await userService.CreateAsync(userDto);
                return Results.Created($"/api/users/{user.UserID}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống", 
                    detail: "Có lỗi xảy ra khi đăng ký tài khoản. Vui lòng thử lại sau.",
                    statusCode: 500
                );
            }
        })
        .WithName("RegisterUser")
        .WithSummary("Đăng ký tài khoản")
        .WithDescription("Tạo tài khoản mới với email và password")
        .Produces<UserResponseDTO>(201)
        .Produces(400);

        // Đăng nhập người dùng với hỗ trợ phiên
        authGroup.MapPost("/login", async (LoginRequestDTO loginDto, IUserService userService, ISessionService sessionService, JwtHelper jwtHelper, HttpContext context) =>
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(loginDto.Email))
                    return Results.BadRequest(new { message = "Email không được để trống" });
                
                if (string.IsNullOrWhiteSpace(loginDto.Password))
                    return Results.BadRequest(new { message = "Mật khẩu không được để trống" });

                var user = await ValidateUserCredentialsAsync(userService, loginDto.Email, loginDto.Password);
                if (user == null)
                {
                    return Results.Json(new { message = "Email hoặc mật khẩu không chính xác" }, statusCode: 401);
                }

                // Tạo phiên với refresh token
                var session = await sessionService.CreateSessionAsync(user.UserID);
                
                // Tạo access token
                var accessToken = jwtHelper.GenerateAccessToken(user);
                
                var response = new AuthResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = session.RefreshToken,
                    AccessTokenExpires = DateTime.UtcNow.AddHours(1), // From config
                    RefreshTokenExpires = session.ExpiresAt,
                    User = new UserResponseDTO
                    {
                        UserID = user.UserID,
                        Email = user.Email,
                        FullName = user.FullName,
                        Role = user.Role,
                        PhoneNumber = user.PhoneNumber,
                        AvatarURL = user.AvatarURL,
                        CreatedAt = user.CreatedAt
                    }
                };

                return Results.Ok(response);
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống", 
                    detail: "Có lỗi xảy ra khi đăng nhập. Vui lòng thử lại sau.",
                    statusCode: 500
                );
            }
        })
        .WithName("LoginUser")
        .WithSummary("Đăng nhập (trả về access + refresh token)")
        .WithDescription("Đăng nhập với email và password để nhận Access Token và Refresh Token")
        .Produces<AuthResponseDTO>(200)
        .Produces(401);

        // Đổi mật khẩu
        authGroup.MapPost("/change-password", async (ChangePasswordDTO changePasswordDto, IUserService userService, HttpContext context) =>
        {
            try
            {
                // Kiểm tra người dùng đã đăng nhập chưa
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                }

                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(changePasswordDto.CurrentPassword))
                    return Results.BadRequest(new { message = "Mật khẩu hiện tại không được để trống" });
                
                if (string.IsNullOrWhiteSpace(changePasswordDto.NewPassword))
                    return Results.BadRequest(new { message = "Mật khẩu mới không được để trống" });
                
                if (changePasswordDto.NewPassword.Length < 6)
                    return Results.BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
                
                if (changePasswordDto.CurrentPassword == changePasswordDto.NewPassword)
                    return Results.BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại" });

                var result = await userService.ChangePasswordAsync(userId, changePasswordDto);
                return result 
                    ? Results.Ok(new { message = "Đổi mật khẩu thành công" }) 
                    : Results.BadRequest(new { message = "Mật khẩu hiện tại không chính xác" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống", 
                    detail: "Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại sau.",
                    statusCode: 500
                );
            }
        })
        .WithName("ChangePassword")
        .WithSummary("Đổi mật khẩu")
        .WithDescription("Đổi mật khẩu cho user đang đăng nhập")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .RequireAuthorization("AuthenticatedOnly");

        // Endpoint làm mới token
        authGroup.MapPost("/refresh", async (RefreshTokenRequestDTO request, ISessionService sessionService, JwtHelper jwtHelper) =>
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return Results.BadRequest(new { message = "Refresh token không được để trống" });

                if (!await sessionService.ValidateRefreshTokenAsync(request.RefreshToken))
                {
                    return Results.Json(new { message = "Refresh token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại." }, statusCode: 401);
                }

                var session = await sessionService.RefreshSessionAsync(request.RefreshToken);
                var accessToken = jwtHelper.GenerateAccessToken(session.User);

                var response = new AuthResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = session.RefreshToken,
                    AccessTokenExpires = DateTime.UtcNow.AddHours(1),
                    RefreshTokenExpires = session.ExpiresAt,
                    User = new UserResponseDTO
                    {
                        UserID = session.User.UserID,
                        Email = session.User.Email,
                        FullName = session.User.FullName,
                        Role = session.User.Role,
                        PhoneNumber = session.User.PhoneNumber,
                        AvatarURL = session.User.AvatarURL,
                        CreatedAt = session.User.CreatedAt
                    }
                };

                return Results.Ok(response);
            }
            catch (InvalidOperationException)
            {
                return Results.Json(new { message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại." }, statusCode: 401);
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống", 
                    detail: "Có lỗi xảy ra khi làm mới token. Vui lòng thử lại sau.",
                    statusCode: 500
                );
            }
        })
        .WithName("RefreshToken")
        .WithSummary("Làm mới access token")
        .WithDescription("Sử dụng refresh token để lấy access token mới")
        .Produces<AuthResponseDTO>(200)
        .Produces(401);

        // Endpoint đăng xuất
        authGroup.MapPost("/logout", async (LogoutRequestDTO request, ISessionService sessionService) =>
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return Results.BadRequest(new { message = "Refresh token không được để trống" });

                var success = await sessionService.RevokeSessionAsync(request.RefreshToken);
                return success 
                    ? Results.Ok(new { message = "Đăng xuất thành công" })
                    : Results.BadRequest(new { message = "Refresh token không hợp lệ hoặc đã được vô hiệu hóa" });
            }
            catch (Exception)
            {
                return Results.Problem(
                    title: "Lỗi hệ thống", 
                    detail: "Có lỗi xảy ra khi đăng xuất. Vui lòng thử lại sau.",
                    statusCode: 500
                );
            }
        })
        .WithName("Logout")
        .WithSummary("Đăng xuất (vô hiệu hóa refresh token)")
        .WithDescription("Đăng xuất và vô hiệu hóa refresh token")
        .Produces(200)
        .Produces(400);

    }

    // Phương thức hỗ trợ để xác thực thông tin đăng nhập
    private static async Task<User?> ValidateUserCredentialsAsync(IUserService userService, string email, string password)
    {
        try
        {
            // Tạm thời sử dụng LoginAsync để xác thực, sau này có thể tách riêng
            var loginDto = new LoginRequestDTO { Email = email, Password = password };
            var token = await userService.LoginAsync(loginDto);
            
            if (string.IsNullOrEmpty(token))
                return null;

            // Nếu đăng nhập thành công, lấy thông tin người dùng
            return await userService.GetByEmailAsync(email);
        }
        catch
        {
            return null;
        }
    }

    // Phương thức hỗ trợ để kiểm tra email hợp lệ
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
