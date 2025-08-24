namespace BE_OPENSKY.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup("/api/users")
            .WithTags("Users")
            .WithOpenApi();

        // Get all users
        userGroup.MapGet("/", async (IUserService userService) =>
        {
            var users = await userService.GetAllAsync();
            return Results.Ok(users);
        })
        .WithName("GetAllUsers")
        .WithSummary("Get all users")
        .Produces<IEnumerable<UserResponseDTO>>();

        // Get user by ID
        userGroup.MapGet("/{id:int}", async (int id, IUserService userService) =>
        {
            var user = await userService.GetByIdAsync(id);
            return user != null ? Results.Ok(user) : Results.NotFound();
        })
        .WithName("GetUserById")
        .WithSummary("Get user by ID")
        .Produces<UserResponseDTO>()
        .Produces(404);

        // Register user
        userGroup.MapPost("/register", async (UserRegisterDTO userDto, IUserService userService) =>
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
        .Produces<UserResponseDTO>(201)
        .Produces(400);

        // Login user
        userGroup.MapPost("/login", async (UserLoginDTO loginDto, IUserService userService) =>
        {
            var token = await userService.LoginAsync(loginDto);
            return token != null 
                ? Results.Ok(new { token, message = "Login successful" }) 
                : Results.Unauthorized();
        })
        .WithName("LoginUser")
        .WithSummary("Login user")
        .Produces(200)
        .Produces(401);

        // Update user
        userGroup.MapPut("/{id:int}", async (int id, UserUpdateDTO userDto, IUserService userService) =>
        {
            var user = await userService.UpdateAsync(id, userDto);
            return user != null ? Results.Ok(user) : Results.NotFound();
        })
        .WithName("UpdateUser")
        .WithSummary("Update user")
        .Produces<UserResponseDTO>()
        .Produces(404)
        .RequireAuthorization();

        // Delete user
        userGroup.MapDelete("/{id:int}", async (int id, IUserService userService) =>
        {
            var result = await userService.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteUser")
        .WithSummary("Delete user")
        .Produces(204)
        .Produces(404)
        .RequireAuthorization("ManagementOnly");

        // Change password
        userGroup.MapPost("/{id:int}/change-password", async (int id, ChangePasswordDTO changePasswordDto, IUserService userService) =>
        {
            var result = await userService.ChangePasswordAsync(id, changePasswordDto);
            return result 
                ? Results.Ok(new { message = "Password changed successfully" }) 
                : Results.BadRequest(new { message = "Current password is incorrect" });
        })
        .WithName("ChangePassword")
        .WithSummary("Change user password")
        .Produces(200)
        .Produces(400)
        .RequireAuthorization("AuthenticatedOnly");
    }
}
