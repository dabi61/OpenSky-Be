namespace BE_OPENSKY.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        // Đăng ký người dùng
        authGroup.MapPost("/register", async ([FromBody] UserRegisterDTO userDto, [FromServices] IUserService userService) =>
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
        authGroup.MapPost("/login", async ([FromBody] LoginRequestDTO loginDto, [FromServices] IUserService userService, [FromServices] ISessionService sessionService, [FromServices] JwtHelper jwtHelper, HttpContext context) =>
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
        authGroup.MapPost("/change-password", async ([FromBody] ChangePasswordDTO changePasswordDto, [FromServices] IUserService userService, HttpContext context) =>
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
        authGroup.MapPost("/refresh", async ([FromBody] RefreshTokenRequestDTO request, [FromServices] ISessionService sessionService, [FromServices] JwtHelper jwtHelper) =>
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

                // Refresh endpoint chỉ trả về access token mới
                var response = new
                {
                    AccessToken = accessToken,
                    AccessTokenExpires = DateTime.UtcNow.AddHours(1)
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
        .WithDescription("Sử dụng refresh token để lấy access token mới. Chỉ trả về access token và thời gian hết hạn.")
        .Produces(200)
        .Produces(401);

        // Password Reset Endpoints
        AddPasswordResetEndpoints(authGroup);

        // Endpoint đăng xuất
        authGroup.MapPost("/logout", async ([FromBody] LogoutRequestDTO request, [FromServices] ISessionService sessionService) =>
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

    // Password Reset Endpoints
    private static void AddPasswordResetEndpoints(RouteGroupBuilder authGroup)
    {
        // Quên mật khẩu
        authGroup.MapPost("/forgot-password", async ([FromBody] ForgotPasswordDTO forgotPasswordDto, [FromServices] IPasswordResetService passwordResetService, [FromServices] IEmailService emailService) =>
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
                    message = emailSent ? 
                        "Mã reset mật khẩu đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư." :
                        "Email service không hoạt động, nhưng token đã được tạo.",
                    // Temporary: Return token if email failed (for testing)
                    token = emailSent ? null : token
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
        authGroup.MapPost("/reset-password", async ([FromBody] ResetPasswordDTO resetPasswordDto, [FromServices] IPasswordResetService passwordResetService) =>
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
        authGroup.MapGet("/validate-reset-token/{token}", async (string token, [FromServices] IPasswordResetService passwordResetService) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Results.BadRequest(new { message = "Token không được để trống" });
                }
                
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
