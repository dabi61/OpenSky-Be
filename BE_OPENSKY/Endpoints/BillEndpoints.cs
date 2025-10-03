using Microsoft.AspNetCore.Mvc;
using BE_OPENSKY.DTOs;
using BE_OPENSKY.Services;
using BE_OPENSKY.Helpers;

namespace BE_OPENSKY.Endpoints
{
    public static class BillEndpoints
    {
        public static void MapBillEndpoints(this IEndpointRouteBuilder app)
        {
            var billGroup = app.MapGroup("/bills")
                .WithTags("Bill")
                .WithOpenApi();

            // GET /bills/my?page=1&size=10 - Lấy danh sách hóa đơn của user đang đăng nhập với phân trang
            billGroup.MapGet("/my", async (
                [FromServices] IBillService billService, 
                HttpContext context,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Json(new { message = "Bạn chưa đăng nhập. Vui lòng đăng nhập trước." }, statusCode: 401);
                    }

                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await billService.GetUserBillsPaginatedAsync(userId, page, size);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetMyBills")
            .WithSummary("Lấy danh sách hóa đơn của tôi")
            .WithDescription("Trả về danh sách hóa đơn thuộc về user đang đăng nhập với phân trang, sắp xếp mới nhất trước")
            .Produces<BillListResponseDTO>(200)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization("AuthenticatedOnly");

            // GET /bills/all?page=1&size=10 - Lấy tất cả bills với phân trang (chỉ Admin/Supervisor)
            billGroup.MapGet("/all", async (
                [FromServices] IBillService billService, 
                HttpContext context,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    // Kiểm tra quyền - chỉ Admin và Supervisor
                    if (!context.User.IsInRole(RoleConstants.Admin) && !context.User.IsInRole(RoleConstants.Supervisor))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này. Chỉ Admin và Supervisor mới được xem tất cả bills." }, statusCode: 403);
                    }

                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var result = await billService.GetAllBillsPaginatedAsync(page, size);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetAllBills")
            .WithSummary("Lấy tất cả bills")
            .WithDescription("Trả về danh sách tất cả bills với phân trang, sắp xếp theo ngày tạo giảm dần. Chỉ Admin và Supervisor mới có quyền truy cập.")
            .Produces<BillListResponseDTO>(200)
            .Produces(403)
            .Produces(500)
            .RequireAuthorization("SupervisorOrAdmin");

            // PUT /bills/apply-voucher - Áp dụng voucher vào bill đã có (billId trong body)
            billGroup.MapPut("/apply-voucher", async (
                ApplyVoucherToBillDTO applyVoucherDto,
                IBillService billService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    // Kiểm tra quyền - Hotel không được áp dụng voucher
                    if (context.User.IsInRole(RoleConstants.Hotel))
                    {
                        return Results.Json(new { message = "Hotel không được phép áp dụng voucher" }, statusCode: 403);
                    }

                    var result = await billService.ApplyVoucherToBillAsync(userId, applyVoucherDto);
                    return Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("ApplyVoucherToBill")
            .WithSummary("Áp dụng voucher vào hóa đơn")
            .WithDescription("Áp dụng voucher giảm giá vào hóa đơn đã có và cập nhật giá tự động (billId trong body)")
            .Produces<ApplyVoucherResponseDTO>(200)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // DELETE /bills/{id}/remove-voucher - Xóa voucher khỏi bill
            billGroup.MapDelete("/{billId}/remove-voucher", async (
                Guid billId,
                IBillService billService,
                HttpContext context) =>
            {
                try
                {
                    // Lấy UserID từ token
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    // Kiểm tra quyền - Hotel không được xóa voucher
                    if (context.User.IsInRole(RoleConstants.Hotel))
                    {
                        return Results.Json(new { message = "Hotel không được phép xóa voucher" }, statusCode: 403);
                    }

                    var result = await billService.RemoveVoucherFromBillAsync(billId, userId);
                    return Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("RemoveVoucherFromBill")
            .WithSummary("Xóa voucher khỏi hóa đơn")
            .WithDescription("Xóa voucher khỏi hóa đơn và khôi phục giá gốc")
            .Produces<ApplyVoucherResponseDTO>(200)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // 1. Tạo QR code thanh toán cho hóa đơn
            billGroup.MapPost("/qr/create", async ([FromBody] QRPaymentRequestDTO request, [FromServices] IQRPaymentService qrPaymentService, HttpContext context) =>
            {
                try
                {
                    var result = await qrPaymentService.CreateQRPaymentAsync(request);
                    return Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("CreateQRPayment")
            .WithSummary("Tạo QR code thanh toán")
            .WithDescription("Tạo QR code để thanh toán hóa đơn")
            .Produces<QRPaymentResponseDTO>(200)
            .Produces(400)
            .Produces(500)
            .RequireAuthorization();

            // 2. Scan QR code để thanh toán
            billGroup.MapPost("/qr/scan", async ([FromBody] QRScanRequestDTO request, [FromServices] IQRPaymentService qrPaymentService, HttpContext context) =>
            {
                try
                {
                    var result = await qrPaymentService.ScanQRPaymentAsync(request.QRCode);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("ScanQRPayment")
            .WithSummary("Scan QR code để thanh toán")
            .WithDescription("Quét QR code để thực hiện thanh toán hóa đơn")
            .Produces<QRPaymentStatusDTO>(200)
            .Produces(400)
            .Produces(500);

            // 3. Kiểm tra trạng thái thanh toán
            billGroup.MapGet("/qr/status/{billId:guid}", async (Guid billId, [FromServices] IQRPaymentService qrPaymentService, HttpContext context) =>
            {
                try
                {
                    var result = await qrPaymentService.GetPaymentStatusAsync(billId);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetPaymentStatus")
            .WithSummary("Kiểm tra trạng thái thanh toán")
            .WithDescription("Kiểm tra trạng thái thanh toán của hóa đơn")
            .Produces<QRPaymentStatusDTO>(200)
            .Produces(500);

            // 4. Lấy thông tin hóa đơn theo ID (User chỉ lấy bill của mình, Admin lấy được mọi bill)
            billGroup.MapGet("/{billId:guid}", async (Guid billId, [FromServices] IBillService billService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var isAdmin = context.User.IsInRole(RoleConstants.Admin);
                    if (isAdmin)
                    {
                        var adminBill = await billService.GetBillByIdAsAdminAsync(billId);
                        if (adminBill == null)
                        {
                            return Results.NotFound(new { message = "Không tìm thấy hóa đơn" });
                        }
                        return Results.Ok(adminBill);
                    }

                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var bill = await billService.GetBillByIdAsync(billId, userIdGuid);
                    if (bill == null)
                    {
                        return Results.NotFound(new { message = "Không tìm thấy hóa đơn hoặc bạn không có quyền truy cập" });
                    }

                    return Results.Ok(bill);
                }
                catch (Exception ex)
                {
                    return Results.Problem(
                        title: "Lỗi hệ thống",
                        detail: ex.Message,
                        statusCode: 500
                    );
                }
            })
            .WithName("GetBillById")
            .WithSummary("Lấy thông tin hóa đơn")
            .WithDescription("Lấy thông tin chi tiết hóa đơn theo ID")
            .Produces<BillResponseDTO>(200)
            .Produces(401)
            .Produces(404)
            .RequireAuthorization();
        }
    }
}
