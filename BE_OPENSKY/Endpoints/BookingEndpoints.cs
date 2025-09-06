using BE_OPENSKY.DTOs;
using BE_OPENSKY.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BE_OPENSKY.Endpoints
{
    public static class BookingEndpoints
    {
        public static void MapBookingEndpoints(this WebApplication app)
        {
            var bookingGroup = app.MapGroup("/api/bookings")
                .WithTags("Booking Management")
                .WithOpenApi();

            // 1. Customer đặt phòng
            bookingGroup.MapPost("/", async (CreateHotelBookingDTO createDto, IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var bookingId = await bookingService.CreateHotelBookingAsync(userIdGuid, createDto);
                    return Results.Ok(new { message = "Đặt phòng thành công", bookingId });
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
            .WithName("CreateHotelBooking")
            .WithSummary("Đặt phòng khách sạn")
            .WithDescription("Customer đặt phòng khách sạn")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .RequireAuthorization();

            // 2. Customer xem booking của mình
            bookingGroup.MapGet("/my-bookings", async (IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var bookings = await bookingService.GetMyBookingsAsync(userIdGuid);
                    return Results.Ok(bookings);
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
            .WithName("GetMyBookings")
            .WithSummary("Xem booking của tôi")
            .WithDescription("Customer xem danh sách booking của mình")
            .Produces<BookingListDTO>(200)
            .Produces(401)
            .RequireAuthorization();

            // 3. Hotel xác nhận booking
            bookingGroup.MapPut("/{bookingId:guid}/confirm", async (Guid bookingId, IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.ConfirmBookingAsync(bookingId, userIdGuid);
                    return success 
                        ? Results.Ok(new { message = "Xác nhận booking thành công" })
                        : Results.NotFound(new { message = "Không tìm thấy booking hoặc bạn không có quyền xác nhận" });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 403);
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
            .WithName("ConfirmBooking")
            .WithSummary("Xác nhận booking")
            .WithDescription("Hotel xác nhận booking và tạo hóa đơn")
            .Produces(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("HotelOnly");

            // 4. Hotel hủy booking
            bookingGroup.MapPut("/{bookingId:guid}/cancel", async (Guid bookingId, IBookingService bookingService, HttpContext context, string? reason = null) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.CancelBookingAsync(bookingId, userIdGuid, reason);
                    return success 
                        ? Results.Ok(new { message = "Hủy booking thành công" })
                        : Results.NotFound(new { message = "Không tìm thấy booking hoặc bạn không có quyền hủy" });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 403);
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
            .WithName("CancelBooking")
            .WithSummary("Hủy booking")
            .WithDescription("Hotel hủy booking")
            .Produces(200)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("HotelOnly");

            // 5. Customer hủy booking
            bookingGroup.MapPut("/{bookingId:guid}/customer-cancel", async (Guid bookingId, IBookingService bookingService, HttpContext context, string? reason = null) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.CustomerCancelBookingAsync(bookingId, userIdGuid, reason);
                    return success 
                        ? Results.Ok(new { message = "Hủy booking thành công" })
                        : Results.NotFound(new { message = "Không tìm thấy booking hoặc không thể hủy" });
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
            .WithName("CustomerCancelBooking")
            .WithSummary("Customer hủy booking")
            .WithDescription("Customer hủy booking của mình")
            .Produces(200)
            .Produces(401)
            .Produces(404)
            .RequireAuthorization();

            // 6. Lấy thông tin booking theo ID
            bookingGroup.MapGet("/{bookingId:guid}", async (Guid bookingId, IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var booking = await bookingService.GetBookingByIdAsync(bookingId, userIdGuid);
                    return booking != null 
                        ? Results.Ok(booking)
                        : Results.NotFound(new { message = "Không tìm thấy booking" });
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
            .WithName("GetBookingById")
            .WithSummary("Xem chi tiết booking")
            .WithDescription("Xem thông tin chi tiết booking")
            .Produces<BookingResponseDTO>(200)
            .Produces(401)
            .Produces(404)
            .RequireAuthorization();

            // 7. Cập nhật trạng thái booking (cho admin/hotel)
            bookingGroup.MapPut("/{bookingId:guid}/status", async (Guid bookingId, UpdateBookingStatusDTO updateDto, IBookingService bookingService, HttpContext context) =>
            {
                try
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
                    {
                        return Results.Json(new { message = "Không tìm thấy thông tin người dùng" }, statusCode: 401);
                    }

                    var success = await bookingService.UpdateBookingStatusAsync(bookingId, userIdGuid, updateDto);
                    return success 
                        ? Results.Ok(new { message = "Cập nhật trạng thái booking thành công" })
                        : Results.NotFound(new { message = "Không tìm thấy booking hoặc bạn không có quyền cập nhật" });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Json(new { message = ex.Message }, statusCode: 403);
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
            .WithName("UpdateBookingStatus")
            .WithSummary("Cập nhật trạng thái booking")
            .WithDescription("Hotel cập nhật trạng thái booking")
            .Produces(200)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("HotelOnly");
        }
    }
}
