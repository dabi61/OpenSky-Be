namespace BE_OPENSKY.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        // Register user
        authGroup.MapPost("/register", async (UserRegisterDTO userDto, IUserService userService) =>
        {
            try
            {
                var user = await userService.CreateAsync(userDto);
                return Results.Created($"/api/users/{user.UserID}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("RegisterUser")
        .WithSummary("Register new user")
        .WithDescription("Tạo tài khoản mới với email và password")
        .Produces<UserResponseDTO>(201)
        .Produces(400);

        // Login user
        authGroup.MapPost("/login", async (UserLoginDTO loginDto, IUserService userService) =>
        {
            var token = await userService.LoginAsync(loginDto);
            return token != null 
                ? Results.Ok(new { token, message = "Login successful" }) 
                : Results.Unauthorized();
        })
        .WithName("LoginUser")
        .WithSummary("Login user")
        .WithDescription("Đăng nhập với email và password để nhận JWT token")
        .Produces(200)
        .Produces(401);

        // Change password
        authGroup.MapPost("/change-password", async (ChangePasswordDTO changePasswordDto, IUserService userService, HttpContext context) =>
        {
            // Lấy user ID từ JWT token
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Results.Unauthorized();
            }

            var result = await userService.ChangePasswordAsync(userId, changePasswordDto);
            return result 
                ? Results.Ok(new { message = "Password changed successfully" }) 
                : Results.BadRequest(new { message = "Current password is incorrect" });
        })
        .WithName("ChangePassword")
        .WithSummary("Change user password")
        .WithDescription("Đổi mật khẩu cho user đang đăng nhập")
        .Produces(200)
        .Produces(400)
        .Produces(401)
        .RequireAuthorization("AuthenticatedOnly");
    }
}
