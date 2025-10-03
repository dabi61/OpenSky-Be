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

            // POST /refunds - Tạo refund request (User) - chuyển sang Pending (Redis)
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
                    var billId = await refundService.CreateRefundRequestAsync(userId, createRefundDto);
                    return Results.Created($"/refunds/pending/{billId}", new { BillID = billId, Message = "Tạo yêu cầu hoàn tiền thành công, chờ duyệt" });
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
            .WithSummary("Tạo refund (Pending)")
            .WithDescription("Tạo yêu cầu refund ở trạng thái chờ duyệt (không hoàn tiền ngay)")
            .Produces<object>(201)
            .Produces(400)
            .Produces(401)
            .Produces(500)
            .RequireAuthorization();

            // GET /refunds/pending/hotel - Danh sách refund Pending của các booking thuộc hotel của tôi (Hotel owner)
            group.MapGet("/pending/hotel", async (
                ApplicationDbContext db,
                HttpContext context,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    // Chỉ role Hotel mới được xem danh sách này
                    if (!context.User.IsInRole(RoleConstants.Hotel))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                    }

                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var myHotelIds = db.Hotels.Where(h => h.UserID == userId).Select(h => h.HotelID);

                    var query = db.Refunds
                        .Include(r => r.Bill)
                            .ThenInclude(b => b.User)
                        .Include(r => r.Bill)
                            .ThenInclude(b => b.Booking)
                        .Where(r => r.Status == RefundStatus.Pending
                                    && r.Bill.Booking != null
                                    && r.Bill.Booking.HotelID != null
                                    && myHotelIds.Contains(r.Bill.Booking.HotelID!.Value))
                        .OrderByDescending(r => r.CreatedAt);

                    var totalCount = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling((double)totalCount / size);

                    var items = await query
                        .Skip((page - 1) * size)
                        .Take(size)
                        .Select(r => new {
                            r.RefundID,
                            r.BillID,
                            r.Description,
                            Status = r.Status.ToString(),
                            r.CreatedAt,
                            BillInfo = new {
                                r.Bill.BillID,
                                r.Bill.TotalPrice,
                                r.Bill.RefundPrice,
                                Status = r.Bill.Status.ToString(),
                                r.Bill.CreatedAt
                            },
                            UserInfo = new {
                                r.Bill.User.UserID,
                                UserName = r.Bill.User.FullName,
                                r.Bill.User.Email
                            }
                        })
                        .ToListAsync();

                    return Results.Ok(new { Refunds = items, TotalCount = totalCount, Page = page, Size = size, TotalPages = totalPages });
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
            .WithName("GetPendingRefundsForHotelOwner")
            .WithSummary("Danh sách refund Pending theo Hotel của tôi")
            .WithDescription("Hotel owner xem các yêu cầu hoàn tiền Pending thuộc các booking của khách trong khách sạn mình")
            .Produces(200)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization("HotelOnly");

            // GET /refunds/pending/tour - Danh sách refund Pending của các booking tour (Supervisor/Admin)
            group.MapGet("/pending/tour", async (
                ApplicationDbContext db,
                HttpContext context,
                int page = 1,
                int size = 10) =>
            {
                try
                {
                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out _))
                    {
                        return Results.Unauthorized();
                    }

                    // Chỉ Supervisor hoặc Admin mới được xem danh sách refund tour
                    if (!context.User.IsInRole(RoleConstants.Supervisor) && !context.User.IsInRole(RoleConstants.Admin))
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                    }

                    if (page < 1) page = 1;
                    if (size < 1 || size > 100) size = 10;

                    var query = db.Refunds
                        .Include(r => r.Bill)
                            .ThenInclude(b => b.User)
                        .Include(r => r.Bill)
                            .ThenInclude(b => b.Booking)
                        .Where(r => r.Status == RefundStatus.Pending
                                    && r.Bill.Booking != null
                                    && r.Bill.Booking.TourID != null)
                        .OrderByDescending(r => r.CreatedAt);

                    var totalCount = await query.CountAsync();
                    var totalPages = (int)Math.Ceiling((double)totalCount / size);

                    var items = await query
                        .Skip((page - 1) * size)
                        .Take(size)
                        .Select(r => new {
                            r.RefundID,
                            r.BillID,
                            r.Description,
                            Status = r.Status.ToString(),
                            r.CreatedAt,
                            BillInfo = new {
                                r.Bill.BillID,
                                r.Bill.TotalPrice,
                                r.Bill.RefundPrice,
                                Status = r.Bill.Status.ToString(),
                                r.Bill.CreatedAt
                            },
                            UserInfo = new {
                                r.Bill.User.UserID,
                                UserName = r.Bill.User.FullName,
                                r.Bill.User.Email
                            }
                        })
                        .ToListAsync();

                    return Results.Ok(new { Refunds = items, TotalCount = totalCount, Page = page, Size = size, TotalPages = totalPages });
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
            .WithName("GetPendingRefundsForTours")
            .WithSummary("Danh sách refund Pending theo Tour (Supervisor/Admin)")
            .WithDescription("Supervisor/Admin xem các yêu cầu hoàn tiền Pending của tất cả booking tour")
            .Produces(200)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization("SupervisorOrAdmin");
            // APPROVE: /refunds/{billId}/approve
            group.MapPut("/{billId:guid}/approve", async (
                Guid billId,
                IRefundService refundService,
                HttpContext context) =>
            {
                try
                {
                    var approverIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (approverIdClaim == null || !Guid.TryParse(approverIdClaim.Value, out var approverId))
                    {
                        return Results.Unauthorized();
                    }

                    var refundId = await refundService.ApproveRefundRequestAsync(billId, approverId);
                    return Results.Ok(new { RefundID = refundId });
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
            .WithName("ApproveRefundRequest")
            .WithSummary("Duyệt yêu cầu hoàn tiền")
            .WithDescription("Chủ Hotel/Tour hoặc Supervisor/Admin duyệt yêu cầu hoàn tiền, thực hiện hoàn tiền và cập nhật DB")
            .Produces<object>(200)
            .Produces(400)
            .Produces(401)
            .RequireAuthorization();

            // REJECT: /refunds/{billId}/reject
            group.MapPut("/{billId:guid}/reject", async (
                Guid billId,
                IRefundService refundService,
                HttpContext context,
                string? reason) =>
            {
                try
                {
                    var approverIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                    if (approverIdClaim == null || !Guid.TryParse(approverIdClaim.Value, out var approverId))
                    {
                        return Results.Unauthorized();
                    }

                    var removed = await refundService.RejectRefundRequestAsync(billId, approverId, reason);
                    return removed ? Results.Ok(new { BillID = billId, Rejected = true }) : Results.NotFound(new { message = "Không có yêu cầu hoàn tiền đang chờ duyệt" });
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
            .WithName("RejectRefundRequest")
            .WithSummary("Từ chối yêu cầu hoàn tiền")
            .WithDescription("Chủ Hotel/Tour hoặc Supervisor/Admin từ chối, hệ thống xóa yêu cầu refund (pending)")
            .Produces<object>(200)
            .Produces(400)
            .Produces(401)
            .Produces(404)
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

            // GET /refunds/status/{status} - Lấy danh sách refund theo status
            group.MapGet("/status/{status}", async (
                RefundStatus status,
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

                    // Kiểm tra quyền - chỉ Admin, Supervisor hoặc Hotel owner mới được xem theo status
                    var isAdmin = context.User.IsInRole(RoleConstants.Admin);
                    var isSupervisor = context.User.IsInRole(RoleConstants.Supervisor);
                    var isHotel = context.User.IsInRole(RoleConstants.Hotel);

                    if (!isAdmin && !isSupervisor && !isHotel)
                    {
                        return Results.Json(new { message = "Bạn không có quyền truy cập chức năng này" }, statusCode: 403);
                    }

                    var result = await refundService.GetRefundsByStatusAsync(status, page, size);
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
            .WithName("GetRefundsByStatus")
            .WithSummary("Lấy danh sách refund theo status")
            .WithDescription("Lấy danh sách yêu cầu hoàn tiền theo trạng thái (Admin/Supervisor/Hotel)")
            .Produces<RefundListResponseDTO>(200)
            .Produces(401)
            .Produces(403)
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
