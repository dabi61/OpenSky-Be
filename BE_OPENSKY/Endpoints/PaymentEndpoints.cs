using BE_OPENSKY.DTOs;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BE_OPENSKY.Endpoints
{
    public static class PaymentEndpoints
    {
        public static void MapPaymentEndpoints(this WebApplication app)
        {
            var paymentGroup = app.MapGroup("/api/payments")
                .WithTags("Payment Management")
                .WithOpenApi();

            // 1. Tạo URL thanh toán VNPay
            paymentGroup.MapPost("/vnpay/create", async ([FromBody] VNPayPaymentRequestDTO request, [FromServices] IVNPayService vnPayService, [FromServices] IBillService billService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    // Kiểm tra bill có tồn tại và thuộc về user không
                    var bill = await billService.GetBillByIdAsync(request.BillId, userIdGuid);
                    if (bill == null)
                    {
                        return Results.NotFound(new { message = "Không tìm thấy hóa đơn hoặc bạn không có quyền truy cập" });
                    }

                    // Kiểm tra bill chưa được thanh toán
                    if (bill.Status == "Paid")
                    {
                        return Results.BadRequest(new { message = "Hóa đơn đã được thanh toán" });
                    }

                    // Tạo URL thanh toán
                    var paymentResponse = await vnPayService.CreatePaymentUrlAsync(request);
                    
                    return Results.Ok(paymentResponse);
                }
                catch (InvalidOperationException ex)
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
            .WithName("CreateVNPayPayment")
            .WithSummary("Tạo URL thanh toán VNPay")
            .WithDescription("Tạo URL thanh toán VNPay cho hóa đơn")
            .Produces<VNPayPaymentResponseDTO>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404)
            .RequireAuthorization();

            // 2. Xử lý callback từ VNPay
            paymentGroup.MapGet("/vnpay-callback", async (
                [FromQuery] string vnp_TxnRef,
                [FromQuery] string vnp_Amount,
                [FromQuery] string vnp_ResponseCode,
                [FromQuery] string vnp_TransactionStatus,
                [FromQuery] string vnp_OrderInfo,
                [FromQuery] string vnp_PayDate,
                [FromQuery] string vnp_TransactionNo,
                [FromQuery] string vnp_BankCode,
                [FromQuery] string vnp_CardType,
                [FromQuery] string vnp_SecureHash,
                [FromServices] IVNPayService vnPayService, 
                [FromServices] IBillService billService, 
                [FromServices] IBookingService bookingService, 
                HttpContext context) =>
            {
                try
                {
                    // Tạo VNPayCallbackDTO từ query parameters
                    var callback = new VNPayCallbackDTO
                    {
                        vnp_TxnRef = vnp_TxnRef,
                        vnp_Amount = vnp_Amount,
                        vnp_ResponseCode = vnp_ResponseCode,
                        vnp_TransactionStatus = vnp_TransactionStatus,
                        vnp_OrderInfo = vnp_OrderInfo,
                        vnp_PayDate = vnp_PayDate,
                        vnp_TransactionNo = vnp_TransactionNo,
                        vnp_BankCode = vnp_BankCode,
                        vnp_CardType = vnp_CardType,
                        vnp_SecureHash = vnp_SecureHash
                    };

                    // Xử lý callback từ VNPay
                    var paymentResult = await vnPayService.ProcessCallbackAsync(callback);
                    
                    if (paymentResult.Success)
                    {
                        // Cập nhật trạng thái bill
                        var billId = Guid.Parse(callback.vnp_TxnRef);
                        await billService.UpdateBillPaymentStatusAsync(billId, "VNPay", callback.vnp_TxnRef, paymentResult.Amount);
                        
                        // Cập nhật trạng thái booking nếu có
                        await bookingService.UpdateBookingPaymentStatusAsync(billId, "Paid");
                    }

                    // Redirect về trang kết quả thanh toán
                    var returnUrl = $"/payment-result?success={paymentResult.Success}&message={Uri.EscapeDataString(paymentResult.Message)}&transactionId={paymentResult.TransactionId}";
                    return Results.Redirect(returnUrl);
                }
                catch (Exception ex)
                {
                    var errorUrl = $"/payment-result?success=false&message={Uri.EscapeDataString($"Lỗi xử lý thanh toán: {ex.Message}")}";
                    return Results.Redirect(errorUrl);
                }
            })
            .WithName("VNPayCallback")
            .WithSummary("Xử lý callback từ VNPay")
            .WithDescription("Xử lý callback từ VNPay sau khi thanh toán")
            .Produces(302)
            .AllowAnonymous();

            // 3. Kiểm tra trạng thái thanh toán
            paymentGroup.MapGet("/status/{transactionId}", async (string transactionId, [FromServices] IVNPayService vnPayService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var paymentResult = await vnPayService.CheckPaymentStatusAsync(transactionId);
                    return Results.Ok(paymentResult);
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
            .WithName("CheckPaymentStatus")
            .WithSummary("Kiểm tra trạng thái thanh toán")
            .WithDescription("Kiểm tra trạng thái thanh toán theo transaction ID")
            .Produces<PaymentResultDTO>(200)
            .Produces(401)
            .RequireAuthorization();

            // 4. Lấy danh sách hóa đơn của user
            paymentGroup.MapGet("/bills", async ([FromServices] IBillService billService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var bills = await billService.GetUserBillsAsync(userIdGuid);
                    return Results.Ok(bills);
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
            .WithName("GetUserBills")
            .WithSummary("Lấy danh sách hóa đơn của user")
            .WithDescription("Lấy danh sách hóa đơn của user hiện tại")
            .Produces<List<BillResponseDTO>>(200)
            .Produces(401)
            .RequireAuthorization();

            // 5. Lấy chi tiết hóa đơn
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
                        return Results.NotFound(new { message = "Không tìm thấy hóa đơn" });
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
            .WithName("GetBillDetails")
            .WithSummary("Lấy chi tiết hóa đơn")
            .WithDescription("Lấy chi tiết hóa đơn theo ID")
            .Produces<BillResponseDTO>(200)
            .Produces(401)
            .Produces(404)
            .RequireAuthorization();

            // 6. Lấy hóa đơn theo booking ID
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
                        return Results.NotFound(new { message = "Không tìm thấy hóa đơn cho booking này" });
                    }

                    // Kiểm tra quyền truy cập
                    if (bill.UserID != userIdGuid)
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập hóa đơn này" }, statusCode: 403);
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
