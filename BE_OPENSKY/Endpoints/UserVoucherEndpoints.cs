using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
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
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
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
            
        }
    }
}
