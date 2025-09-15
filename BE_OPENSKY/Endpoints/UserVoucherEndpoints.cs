using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints
{
    public static class UserVoucherEndpoints
    {
        public static void MapUserVoucherEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/user_vouchers")
                .WithTags("UserVoucher")
                .WithOpenApi();

            // POST /user_vouchers - Lưu voucher (người dùng)
            group.MapPost("/", async (
                SaveVoucherDTO saveVoucherDto,
                IUserVoucherService userVoucherService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Không thể xác định người dùng" }, statusCode: 401);
                    }

                    // Kiểm tra dữ liệu đầu vào
                    if (saveVoucherDto.VoucherID == Guid.Empty)
                    {
                        return Results.Json(new { message = "VoucherID không hợp lệ" }, statusCode: 400);
                    }

                    // Kiểm tra voucher đã được lưu chưa
                    var alreadySaved = await userVoucherService.IsVoucherAlreadySavedAsync(userId, saveVoucherDto.VoucherID);
                    if (alreadySaved)
                    {
                        return Results.Json(new { message = "Bạn đã lưu voucher này rồi" }, statusCode: 400);
                    }

                    var userVoucherId = await userVoucherService.SaveVoucherAsync(userId, saveVoucherDto);

                    return Results.Json(new { 
                        message = "Lưu voucher thành công", 
                        userVoucherId = userVoucherId 
                    }, statusCode: 201);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lưu voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("SaveVoucher")
            .WithSummary("Lưu voucher")
            .WithDescription("Người dùng lưu voucher vào danh sách của mình")
            .Produces(201)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization("AuthenticatedOnly");

            // GET /user_vouchers?page=1&size=10 - Lấy danh sách user voucher (Admin)
            group.MapGet("/", async (
                IUserVoucherService userVoucherService,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await userVoucherService.GetUserVouchersAsync(page, size);

                    return Results.Json(new
                    {
                        message = "Lấy danh sách user voucher thành công",
                        data = result
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách user voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetUserVouchers")
            .WithSummary("Lấy danh sách user voucher")
            .WithDescription("Lấy danh sách user voucher có phân trang (Admin)")
            .Produces(200)
            .Produces(500)
            .RequireAuthorization("AdminOnly");

            // GET /user_vouchers/my-vouchers?page=1&size=10 - Lấy voucher của tôi
            group.MapGet("/my-vouchers", async (
                IUserVoucherService userVoucherService,
                HttpContext context,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Không thể xác định người dùng" }, statusCode: 401);
                    }

                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await userVoucherService.GetUserVouchersByUserIdAsync(userId, page, size);

                    return Results.Json(new
                    {
                        message = "Lấy danh sách voucher của tôi thành công",
                        data = result
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách voucher của tôi: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetMyVouchers")
            .WithSummary("Lấy voucher của tôi")
            .WithDescription("Lấy danh sách voucher đã lưu của người dùng hiện tại")
            .Produces(200)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization("AuthenticatedOnly");

            // GET /user_vouchers/{id} - Lấy user voucher theo ID
            group.MapGet("/{id:guid}", async (
                Guid id,
                IUserVoucherService userVoucherService) =>
            {
                try
                {
                    var userVoucher = await userVoucherService.GetUserVoucherByIdAsync(id);
                    if (userVoucher == null)
                    {
                        return Results.Json(new { message = "Không tìm thấy user voucher" }, statusCode: 404);
                    }

                    return Results.Json(new
                    {
                        message = "Lấy user voucher thành công",
                        data = userVoucher
                    });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy user voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetUserVoucherById")
            .WithSummary("Lấy user voucher theo ID")
            .WithDescription("Lấy thông tin chi tiết user voucher theo ID")
            .Produces(200)
            .Produces(404)
            .Produces(500);

            // PUT /user_vouchers/{id} - Cập nhật user voucher
            group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateUserVoucherDTO updateUserVoucherDto,
                IUserVoucherService userVoucherService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Không thể xác định người dùng" }, statusCode: 401);
                    }

                    var success = await userVoucherService.UpdateUserVoucherAsync(id, updateUserVoucherDto);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy user voucher hoặc không thể cập nhật" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Cập nhật user voucher thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi cập nhật user voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("UpdateUserVoucher")
            .WithSummary("Cập nhật user voucher")
            .WithDescription("Cập nhật thông tin user voucher")
            .Produces(200)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("AuthenticatedOnly");

            // PUT /user_vouchers/{id}/use - Sử dụng voucher
            group.MapPut("/{id:guid}/use", async (
                Guid id,
                IUserVoucherService userVoucherService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Không thể xác định người dùng" }, statusCode: 401);
                    }

                    var success = await userVoucherService.UseVoucherAsync(id);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không thể sử dụng voucher. Voucher có thể đã hết hạn, đã sử dụng hoặc không tồn tại." }, statusCode: 400);
                    }

                    return Results.Json(new { message = "Sử dụng voucher thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi sử dụng voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("UseVoucher")
            .WithSummary("Sử dụng voucher")
            .WithDescription("Sử dụng voucher (đánh dấu đã sử dụng)")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization("AuthenticatedOnly");

            // DELETE /user_vouchers/{id} - Xóa user voucher
            group.MapDelete("/{id:guid}", async (
                Guid id,
                IUserVoucherService userVoucherService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst("user_id");
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Không thể xác định người dùng" }, statusCode: 401);
                    }

                    var success = await userVoucherService.DeleteUserVoucherAsync(id);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy user voucher hoặc không thể xóa" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Xóa user voucher thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi xóa user voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("DeleteUserVoucher")
            .WithSummary("Xóa user voucher")
            .WithDescription("Xóa voucher khỏi danh sách của người dùng")
            .Produces(200)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("AuthenticatedOnly");
        }
    }
}
