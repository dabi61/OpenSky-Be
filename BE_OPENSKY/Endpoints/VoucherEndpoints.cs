using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints
{
    public static class VoucherEndpoints
    {
        public static void MapVoucherEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/vouchers")
                .WithTags("Voucher")
                .WithOpenApi();

            // POST /vouchers - Tạo voucher mới (chỉ Admin)
            group.MapPost("/", async (
                CreateVoucherDTO createVoucherDto,
                IVoucherService voucherService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin
                    if (!context.User.IsInRole(RoleConstants.Admin))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin mới được tạo voucher." }, statusCode: 403);
                    }

                    // Kiểm tra dữ liệu đầu vào
                    if (string.IsNullOrWhiteSpace(createVoucherDto.Code))
                    {
                        return Results.Json(new { message = "Mã voucher không được để trống" }, statusCode: 400);
                    }

                    if (createVoucherDto.Percent <= 0 || createVoucherDto.Percent > 100)
                    {
                        return Results.Json(new { message = "Phần trăm giảm giá phải từ 1 đến 100" }, statusCode: 400);
                    }

                    if (createVoucherDto.StartDate >= createVoucherDto.EndDate)
                    {
                        return Results.Json(new { message = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc" }, statusCode: 400);
                    }

                    // Bỏ kiểm tra MaxUsage: không sử dụng trong API

                    // Kiểm tra mã voucher đã tồn tại chưa
                    var codeExists = await voucherService.IsVoucherCodeExistsAsync(createVoucherDto.Code);
                    if (codeExists)
                    {
                        return Results.Json(new { message = "Mã voucher đã tồn tại" }, statusCode: 400);
                    }

                    var voucherId = await voucherService.CreateVoucherAsync(createVoucherDto);

                    return Results.Json(new { 
                        message = "Tạo voucher thành công", 
                        voucherId = voucherId 
                    }, statusCode: 201);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi tạo voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("CreateVoucher")
            .WithSummary("Tạo voucher mới")
            .WithDescription("Tạo voucher mới. Chỉ Admin mới được tạo voucher.")
            .Produces(201)
            .Produces(400)
            .Produces(403)
            .Produces(500)
            .RequireAuthorization("AdminOnly");

            // GET /vouchers?page=1&size=10 - Lấy danh sách voucher có phân trang
            group.MapGet("/", async (
                IVoucherService voucherService,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await voucherService.GetVouchersAsync(page, size);

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetVouchers")
            .WithSummary("Lấy danh sách voucher")
            .WithDescription("Lấy danh sách voucher có phân trang")
            .Produces(200)
            .Produces(500);

            // GET /vouchers/{id} - Lấy voucher theo ID
            group.MapGet("/{id:guid}", async (
                Guid id,
                IVoucherService voucherService) =>
            {
                try
                {
                    var voucher = await voucherService.GetVoucherByIdAsync(id);
                    if (voucher == null)
                    {
                        return Results.Json(new { message = "Không tìm thấy voucher" }, statusCode: 404);
                    }

                    return Results.Ok(voucher);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetVoucherById")
            .WithSummary("Lấy voucher theo ID")
            .WithDescription("Lấy thông tin chi tiết voucher theo ID")
            .Produces(200)
            .Produces(404)
            .Produces(500);

            // GET /vouchers/active?page=1&size=10 - Lấy voucher đang hoạt động
            group.MapGet("/active", async (
                IVoucherService voucherService,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await voucherService.GetActiveVouchersAsync(page, size);

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách voucher đang hoạt động: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetActiveVouchers")
            .WithSummary("Lấy voucher đang hoạt động")
            .WithDescription("Lấy danh sách voucher đang trong thời gian hiệu lực")
            .Produces(200)
            .Produces(500);

            // GET /vouchers/type/{tableType}?page=1&size=10 - Lấy voucher theo loại
            group.MapGet("/type/{tableType}", async (
                TableType tableType,
                IVoucherService voucherService,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await voucherService.GetVouchersByTableTypeAsync(tableType, page, size);

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi lấy danh sách voucher theo loại: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("GetVouchersByTableType")
            .WithSummary("Lấy voucher theo loại")
            .WithDescription("Lấy danh sách voucher theo loại (Tour hoặc Hotel)")
            .Produces(200)
            .Produces(500);

            // PUT /vouchers/{id} - Cập nhật voucher (chỉ Admin)
            group.MapPut("/{id:guid}", async (
                Guid id,
                UpdateVoucherDTO updateVoucherDto,
                IVoucherService voucherService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin
                    if (!context.User.IsInRole(RoleConstants.Admin))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin mới được cập nhật voucher." }, statusCode: 403);
                    }

                    // Kiểm tra dữ liệu đầu vào
                    if (!string.IsNullOrWhiteSpace(updateVoucherDto.Code))
                    {
                        var codeExists = await voucherService.IsVoucherCodeExistsAsync(updateVoucherDto.Code);
                        if (codeExists)
                        {
                            return Results.Json(new { message = "Mã voucher đã tồn tại" }, statusCode: 400);
                        }
                    }

                    if (updateVoucherDto.Percent.HasValue && (updateVoucherDto.Percent.Value <= 0 || updateVoucherDto.Percent.Value > 100))
                    {
                        return Results.Json(new { message = "Phần trăm giảm giá phải từ 1 đến 100" }, statusCode: 400);
                    }

                    if (updateVoucherDto.StartDate.HasValue && updateVoucherDto.EndDate.HasValue)
                    {
                        if (updateVoucherDto.StartDate.Value >= updateVoucherDto.EndDate.Value)
                        {
                            return Results.Json(new { message = "Ngày bắt đầu phải nhỏ hơn ngày kết thúc" }, statusCode: 400);
                        }
                    }

                    // Bỏ kiểm tra MaxUsage trong update

                    var success = await voucherService.UpdateVoucherAsync(id, updateVoucherDto);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy voucher hoặc không thể cập nhật" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Cập nhật voucher thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi cập nhật voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("UpdateVoucher")
            .WithSummary("Cập nhật voucher")
            .WithDescription("Cập nhật thông tin voucher. Chỉ Admin mới được cập nhật voucher.")
            .Produces(200)
            .Produces(400)
            .Produces(403)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("AdminOnly");

            // PUT /vouchers/{id}/delete - Soft delete voucher (chỉ Admin)
            group.MapPut("/{id:guid}/delete", async (
                Guid id,
                IVoucherService voucherService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin
                    if (!context.User.IsInRole(RoleConstants.Admin))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin mới được xóa voucher." }, statusCode: 403);
                    }

                    var success = await voucherService.DeleteVoucherAsync(id);
                    if (!success)
                    {
                        return Results.Json(new { message = "Không tìm thấy voucher hoặc không thể xóa" }, statusCode: 404);
                    }

                    return Results.Json(new { message = "Xóa voucher thành công" });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { message = $"Lỗi khi xóa voucher: {ex.Message}" }, statusCode: 500);
                }
            })
            .WithName("DeleteVoucher")
            .WithSummary("Xóa voucher (soft delete)")
            .WithDescription("Xóa voucher (soft delete). Chỉ Admin mới được xóa voucher.")
            .Produces(200)
            .Produces(403)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("AdminOnly");
        }
    }
}
