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

            // 4. Lấy thông tin hóa đơn theo ID
            billGroup.MapGet("/{billId:guid}", async (Guid billId, [FromServices] IBillService billService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
