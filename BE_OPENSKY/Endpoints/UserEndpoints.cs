namespace BE_OPENSKY.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var userGroup = app.MapGroup("/api/users")
            .WithTags("Users")
            .WithOpenApi();

        // Lấy danh sách tất cả người dùng
        userGroup.MapGet("/", async (IUserService userService) =>
        {
            var users = await userService.GetAllAsync();
            return Results.Ok(users);
        })
        .WithName("GetAllUsers")
        .WithSummary("Lấy danh sách người dùng")
        .WithDescription("Lấy thông tin tất cả người dùng trong hệ thống")
        .Produces<IEnumerable<UserResponseDTO>>();

        // Lấy người dùng theo ID
        userGroup.MapGet("/{id:guid}", async (Guid id, IUserService userService) =>
        {
            var user = await userService.GetByIdAsync(id);
            return user != null ? Results.Ok(user) : Results.NotFound();
        })
        .WithName("GetUserById")
        .WithSummary("Lấy người dùng theo ID")
        .WithDescription("Lấy thông tin chi tiết người dùng theo ID")
        .Produces<UserResponseDTO>()
        .Produces(404);

        // Lưu ý: Các endpoint xác thực đã chuyển sang nhóm /api/auth

        // Cập nhật thông tin người dùng
        userGroup.MapPut("/{id:guid}", async (Guid id, UserUpdateDTO userDto, IUserService userService) =>
        {
            var user = await userService.UpdateAsync(id, userDto);
            return user != null ? Results.Ok(user) : Results.NotFound();
        })
        .WithName("UpdateUser")
        .WithSummary("Cập nhật người dùng")
        .WithDescription("Chỉnh sửa thông tin người dùng")
        .Produces<UserResponseDTO>()
        .Produces(404)
        .RequireAuthorization();

        // Xóa người dùng
        userGroup.MapDelete("/{id:guid}", async (Guid id, IUserService userService) =>
        {
            var result = await userService.DeleteAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteUser")
        .WithSummary("Xóa người dùng")
        .WithDescription("Xóa người dùng khỏi hệ thống (chỉ Management)")
        .Produces(204)
        .Produces(404)
        .RequireAuthorization("ManagementOnly");

        // Lưu ý: Endpoint đổi mật khẩu đã chuyển sang nhóm /api/auth
    }
}
