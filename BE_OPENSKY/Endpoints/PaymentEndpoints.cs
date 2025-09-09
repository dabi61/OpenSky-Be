using BE_OPENSKY.DTOs;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BE_OPENSKY.Endpoints
{
    public static class PaymentEndpoints
    {
        public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
        {
            var paymentGroup = app.MapGroup("/payments")
                .WithTags("Payments")
                .WithOpenApi();

            // QR Payment endpoints (Test đơn giản)
            // 1. Tạo QR code thanh toán
            paymentGroup.MapPost("/qr/create", async ([FromBody] QRPaymentRequestDTO request, [FromServices] IQRPaymentService qrPaymentService, HttpContext context) =>
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
            .WithDescription("Tạo QR code để thanh toán hóa đơn (Test đơn giản)")
            .Produces<QRPaymentResponseDTO>(200)
            .Produces(400)
            .Produces(500)
            .RequireAuthorization();

            // 2. Quét QR code để thanh toán
            paymentGroup.MapGet("/qr/scan", async ([FromQuery] string code, [FromServices] IQRPaymentService qrPaymentService, HttpContext context) =>
            {
                try
                {
                    var result = await qrPaymentService.ScanQRPaymentAsync(code);
                    
                    // Trả về trang HTML đơn giản
                    var html = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Kết quả thanh toán</title>
                        <meta charset='utf-8'>
                        <style>
                            body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                            .success {{ color: green; font-size: 24px; }}
                            .error {{ color: red; font-size: 24px; }}
                            .info {{ color: blue; font-size: 18px; }}
                        </style>
                    </head>
                    <body>
                        <h1>Kết quả thanh toán</h1>
                        <div class='{(result.Status == "Paid" ? "success" : "error")}'>
                            {result.Message}
                        </div>
                        <div class='info'>
                            <p>Trạng thái: {result.Status}</p>
                            {(result.PaidAt.HasValue ? $"<p>Thời gian: {result.PaidAt:dd/MM/yyyy HH:mm:ss}</p>" : "")}
                        </div>
                        <button onclick='window.close()'>Đóng</button>
                    </body>
                    </html>";
                    
                    return Results.Content(html, "text/html");
                }
                catch (Exception ex)
                {
                    var errorHtml = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>Lỗi thanh toán</title>
                        <meta charset='utf-8'>
                        <style>
                            body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
                            .error {{ color: red; font-size: 24px; }}
                        </style>
                    </head>
                    <body>
                        <h1>Lỗi thanh toán</h1>
                        <div class='error'>
                            {ex.Message}
                        </div>
                        <button onclick='window.close()'>Đóng</button>
                    </body>
                    </html>";
                    
                    return Results.Content(errorHtml, "text/html");
                }
            })
            .WithName("ScanQRPayment")
            .WithSummary("Quét QR code thanh toán")
            .WithDescription("Quét QR code để thực hiện thanh toán (Test đơn giản)")
            .Produces(200)
            .Produces(500);

            // 3. Kiểm tra trạng thái thanh toán QR
            paymentGroup.MapGet("/qr/status/{billId:guid}", async (Guid billId, [FromServices] IQRPaymentService qrPaymentService, HttpContext context) =>
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
            .WithName("GetQRPaymentStatus")
            .WithSummary("Kiểm tra trạng thái thanh toán QR")
            .WithDescription("Kiểm tra trạng thái thanh toán QR code")
            .Produces<QRPaymentStatusDTO>(200)
            .Produces(500)
            .RequireAuthorization();

            // 4. Lấy thông tin hóa đơn theo ID
            paymentGroup.MapGet("/bills/{billId:guid}", async (Guid billId, [FromServices] IBillService billService, HttpContext context) =>
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
            .WithName("GetBill")
            .WithSummary("Lấy thông tin hóa đơn")
            .WithDescription("Lấy thông tin chi tiết hóa đơn theo ID")
            .Produces<BillResponseDTO>(200)
            .Produces(401)
            .Produces(404)
            .RequireAuthorization();

            // 5. Lấy hóa đơn theo booking ID
            paymentGroup.MapGet("/bills/booking/{bookingId:guid}", async (Guid bookingId, [FromServices] IBillService billService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var bill = await billService.GetBillByBookingIdAsync(bookingId);
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
            .WithName("GetBillByBookingId")
            .WithSummary("Lấy hóa đơn theo booking ID")
            .WithDescription("Lấy hóa đơn theo booking ID")
            .Produces<BillResponseDTO>(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization();
        }
    }
}