using Microsoft.AspNetCore.Mvc;

namespace BE_OPENSKY.Endpoints
{
    public static class BillEndpoints
    {
        public static void MapBillEndpoints(this IEndpointRouteBuilder app)
        {
            var billGroup = app.MapGroup("/bills")
                .WithTags("Bill")
                .WithOpenApi();

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

            // 2. Lấy thông tin hóa đơn theo ID
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
