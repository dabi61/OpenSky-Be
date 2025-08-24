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

        // Note: Authentication endpoints moved to /api/auth group

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

        // Note: Change password endpoint moved to /api/auth group
    }
}
