using BE_OPENSKY.DTOs;
using BE_OPENSKY.Helpers;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;

namespace BE_OPENSKY.Endpoints
{
    public static class RefundEndpoints
    {
        public static void MapRefundEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/refunds")
                .WithTags("Refund")
                .WithOpenApi();

            // POST /refunds - Tạo refund request (User)
            group.MapPost("/", async (
                CreateRefundDTO createRefundDto,
                IRefundService refundService,
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

                    var refundId = await refundService.CreateRefundAsync(userId, createRefundDto);
                    return Results.Created($"/refunds/{refundId}", new { RefundID = refundId, Message = "Tạo yêu cầu hoàn tiền thành công" });
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
            .WithName("CreateRefund")
            .WithSummary("Tạo refund (tự động hoàn tiền)")
            .WithDescription("Tạo refund và tự động hoàn tiền ngay lập tức cho hóa đơn đã thanh toán")
            .Produces<object>(201)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // GET /refunds/{id} - Lấy refund theo ID
            group.MapGet("/{refundId}", async (
                Guid refundId,
                IRefundService refundService,
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

                    var refund = await refundService.GetRefundByIdAsync(refundId, userId);
                    if (refund == null)
                        return Results.NotFound(new { message = "Không tìm thấy yêu cầu hoàn tiền" });

                    return Results.Ok(refund);
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
            .WithName("GetRefundById")
            .WithSummary("Lấy thông tin refund theo ID")
            .WithDescription("Lấy chi tiết thông tin yêu cầu hoàn tiền")
            .Produces<RefundResponseDTO>(200)
            .Produces(401)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization();

            // GET /refunds - Lấy danh sách refund của user
            group.MapGet("/", async (
                IRefundService refundService,
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
                        return Results.Unauthorized();
                    }

                    // Kiểm tra quyền admin
                    var isAdmin = context.User.IsInRole(RoleConstants.Admin);
                    
                    RefundListResponseDTO result;
                    if (isAdmin)
                    {
                        result = await refundService.GetAllRefundsAsync(page, size);
                    }
                    else
                    {
                        result = await refundService.GetUserRefundsAsync(userId, page, size);
                    }

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
            .WithName("GetRefunds")
            .WithSummary("Lấy danh sách refund")
            .WithDescription("Lấy danh sách yêu cầu hoàn tiền (User: của mình, Admin: tất cả)")
            .Produces<RefundListResponseDTO>(200)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();


            // GET /refunds/stats - Thống kê refund (Admin)
            group.MapGet("/stats", async (
                IRefundService refundService,
                HttpContext context) =>
            {
                try
                {
                    // Kiểm tra quyền Admin
                    if (!context.User.IsInRole(RoleConstants.Admin))
                    {
                        return Results.Forbid();
                    }

                    var stats = await refundService.GetRefundStatsAsync();
                    return Results.Ok(stats);
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
            .WithName("GetRefundStats")
            .WithSummary("Thống kê refund")
            .WithDescription("Lấy thống kê về các yêu cầu hoàn tiền (chỉ Admin)")
            .Produces<RefundStatsDTO>(200)
            .Produces(401)
            .Produces(403)
            .Produces(500)
            .RequireAuthorization("AdminOnly");

        }
    }
}
